using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Communication.Telegram.Abstractions;
using Pottmayer.Tars.Communication.Telegram.Abstractions.Models;
using Pottmayer.Tars.Communication.Telegram.Options;
using Pottmayer.Tars.Communication.Telegram.Wire;

namespace Pottmayer.Tars.Communication.Telegram;

/// <summary>
/// Talks to the Telegram Bot API over HTTP. Stateless, so it is safe as a singleton, and it holds no
/// polling offset — that is the caller's state to persist.
/// </summary>
public sealed class TelegramBotClient(HttpClient http, IOptions<TelegramOptions> options) : ITelegramClient
{
    public const string ProviderName = "telegram";

    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<TelegramSendResult> SendMessageAsync(
        TelegramMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var sent = await CallAsync<WireMessage>(
            "sendMessage", TelegramWireMapper.ToSendRequest(message), options.Value.RequestTimeout, cancellationToken);

        return new TelegramSendResult(
            message.ChatId, sent.MessageId, DateTimeOffset.FromUnixTimeSeconds(sent.Date));
    }

    public async Task AnswerCallbackQueryAsync(
        string callbackQueryId, string? text = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(callbackQueryId);

        var request = new AnswerCallbackQueryRequest { CallbackQueryId = callbackQueryId, Text = text };
        await CallAsync<bool>("answerCallbackQuery", request, options.Value.RequestTimeout, cancellationToken);
    }

    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(
        long offset, TimeSpan pollTimeout, CancellationToken cancellationToken = default)
    {
        if (pollTimeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(pollTimeout), "Poll timeout must not be negative.");

        var request = new GetUpdatesRequest
        {
            Offset = offset,
            Timeout = (int)pollTimeout.TotalSeconds,
        };

        // The request must outlive the long poll itself, or the transport cancels what it is waiting for.
        var httpTimeout = pollTimeout + options.Value.PollTimeoutGrace;

        var updates = await CallAsync<List<WireUpdate>>("getUpdates", request, httpTimeout, cancellationToken);

        return [.. updates.Select(TelegramWireMapper.ToUpdate)];
    }

    /// <summary>
    /// Downloads the file into memory. Against the public Bot API this is safe by construction: it
    /// caps downloads at 20 MB, and buffering keeps the HTTP response fully disposed before the
    /// caller ever touches the stream, rather than leaving a connection open on an ownership
    /// technicality.
    /// </summary>
    /// <remarks>
    /// The 20 MB ceiling is the whole justification, so it stops holding against a self-hosted Bot
    /// API server (<see cref="TelegramOptions.ApiBaseUrl"/>), where the limit rises to 2 GB and a
    /// large file becomes an <see cref="OutOfMemoryException"/>. A streaming overload is the fix when
    /// a caller actually needs one; until then, this method is for small attachments.
    /// </remarks>
    public async Task<Stream> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        var settings = options.Value;

        var file = await CallAsync<WireFile>(
            "getFile", new GetFileRequest { FileId = fileId }, settings.RequestTimeout, cancellationToken);

        if (string.IsNullOrEmpty(file.FilePath))
        {
            throw new TelegramException(
                "getFile", $"Telegram returned no file path for file id '{fileId}'.", isPermanent: true);
        }

        var url = $"{settings.ApiBaseUrl.TrimEnd('/')}/file/bot{settings.BotToken}/{file.FilePath}";

        using var timeout = CreateTimeout(settings.RequestTimeout, cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(url, timeout.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new TelegramException(
                "getFile", $"Could not reach the Telegram file endpoint: {ex.Message}", isPermanent: false, innerException: ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                throw new TelegramException(
                    "getFile",
                    $"Downloading file '{fileId}' failed with HTTP {status}.",
                    TelegramErrorClassifier.IsPermanent(status),
                    status);
            }

            var buffer = new MemoryStream();
            await response.Content.CopyToAsync(buffer, timeout.Token);
            buffer.Position = 0;
            return buffer;
        }
    }

    public async Task SetWebhookAsync(Uri url, string secretToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretToken);

        var request = new SetWebhookRequest { Url = url.ToString(), SecretToken = secretToken };
        await CallAsync<bool>("setWebhook", request, options.Value.RequestTimeout, cancellationToken);
    }

    public async Task DeleteWebhookAsync(CancellationToken cancellationToken = default)
        => await CallAsync<bool>("deleteWebhook", new object(), options.Value.RequestTimeout, cancellationToken);

    /// <summary>
    /// Posts one Bot API method and unwraps the <c>{ ok, result }</c> envelope, turning every failure
    /// — transport, HTTP status or <c>ok: false</c> — into a classified <see cref="TelegramException"/>.
    /// </summary>
    private async Task<TResult> CallAsync<TResult>(
        string method, object request, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.BotToken))
            throw new InvalidOperationException($"{TelegramOptions.SectionName}:BotToken is not configured.");

        var url = $"{settings.ApiBaseUrl.TrimEnd('/')}/bot{settings.BotToken}/{method}";

        using var timeoutSource = CreateTimeout(timeout, cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(url, request, Json, timeoutSource.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            // Unreachable host, DNS, TLS, or our own timeout: none of it says the request was bad.
            throw new TelegramException(
                method, $"Could not reach the Telegram Bot API: {ex.Message}", isPermanent: false, innerException: ex);
        }

        using (response)
        {
            BotApiResponse<TResult>? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<BotApiResponse<TResult>>(Json, timeoutSource.Token);
            }
            catch (JsonException ex)
            {
                throw new TelegramException(
                    method,
                    $"Telegram returned a body that is not a Bot API response (HTTP {(int)response.StatusCode}).",
                    isPermanent: false,
                    (int)response.StatusCode,
                    innerException: ex);
            }

            if (payload is null)
            {
                throw new TelegramException(
                    method, $"Telegram returned an empty body (HTTP {(int)response.StatusCode}).", isPermanent: false);
            }

            if (!payload.Ok)
                throw ToException(method, payload, (int)response.StatusCode);

            if (payload.Result is null)
            {
                throw new TelegramException(
                    method, "Telegram reported success but returned no result.", isPermanent: false);
            }

            return payload.Result;
        }
    }

    private static TelegramException ToException<TResult>(string method, BotApiResponse<TResult> payload, int httpStatus)
    {
        // error_code repeats the HTTP status, but trust the body first: it is what Telegram documents.
        var code = payload.ErrorCode ?? httpStatus;
        var retryAfter = payload.Parameters?.RetryAfter is { } seconds
            ? TimeSpan.FromSeconds(seconds)
            : (TimeSpan?)null;

        var description = string.IsNullOrWhiteSpace(payload.Description) ? "(no description)" : payload.Description;

        return new TelegramException(
            method,
            $"Telegram rejected {method} with error {code}: {description}",
            TelegramErrorClassifier.IsPermanent(code),
            code,
            retryAfter);
    }

    private static CancellationTokenSource CreateTimeout(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout > TimeSpan.Zero)
            source.CancelAfter(timeout);

        return source;
    }
}
