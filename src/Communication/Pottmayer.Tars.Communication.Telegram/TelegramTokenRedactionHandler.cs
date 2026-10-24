using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Communication.Telegram.Options;
using Pottmayer.Tars.Observability.Abstractions;

namespace Pottmayer.Tars.Communication.Telegram;

/// <summary>
/// Attaches the request URL with the bot token masked (<c>/bot***/</c>) as
/// <see cref="TarsTelemetry.RedactedRequestUrl"/>, which <see cref="TelegramHttpClientLogger"/> and the tars
/// HttpClient tracing record instead of the real one. Off when <see cref="TelegramOptions.RedactBotToken"/> is.
/// </summary>
internal sealed partial class TelegramTokenRedactionHandler(IOptionsMonitor<TelegramOptions> options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (options.CurrentValue.RedactBotToken && request.RequestUri is { } uri)
            request.Options.Set(TarsTelemetry.RedactedRequestUrl, Redact(uri));

        return base.SendAsync(request, cancellationToken);
    }

    /// <summary>Masks the <c>bot{token}</c> path segment of method calls and file downloads.</summary>
    internal static string Redact(Uri uri) => BotTokenSegment().Replace(uri.AbsoluteUri, "/bot***/");

    [GeneratedRegex("/bot[^/]+/")]
    private static partial Regex BotTokenSegment();
}
