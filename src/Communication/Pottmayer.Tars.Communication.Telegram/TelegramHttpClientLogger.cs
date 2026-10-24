using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Pottmayer.Tars.Observability.Abstractions;

namespace Pottmayer.Tars.Communication.Telegram;

/// <summary>
/// Replaces the default <see cref="IHttpClientFactory"/> request logging of the Telegram client, which logs
/// <see cref="HttpRequestMessage.RequestUri"/> and with it the bot token. Logs the same events with
/// <see cref="TarsTelemetry.RedactedRequestUrl"/> when <see cref="TelegramTokenRedactionHandler"/> set it.
/// </summary>
internal sealed class TelegramHttpClientLogger(ILogger logger) : IHttpClientLogger
{
    public object? LogRequestStart(HttpRequestMessage request)
    {
        logger.LogDebug("Sending HTTP request {HttpMethod} {Uri}", request.Method, Url(request));
        return null;
    }

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
        => logger.LogInformation(
            "HTTP request {HttpMethod} {Uri} responded {StatusCode} after {ElapsedMilliseconds}ms",
            request.Method, Url(request), (int)response.StatusCode, elapsed.TotalMilliseconds);

    public void LogRequestFailed(
        object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
        => logger.LogWarning(
            exception, "HTTP request {HttpMethod} {Uri} failed after {ElapsedMilliseconds}ms",
            request.Method, Url(request), elapsed.TotalMilliseconds);

    private static string? Url(HttpRequestMessage request)
        => request.Options.TryGetValue(TarsTelemetry.RedactedRequestUrl, out var url) ? url : request.RequestUri?.ToString();
}
