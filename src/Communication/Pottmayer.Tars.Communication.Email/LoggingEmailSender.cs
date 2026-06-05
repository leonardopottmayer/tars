using Microsoft.Extensions.Logging;
using Pottmayer.Tars.Communication.Email.Abstractions;

namespace Pottmayer.Tars.Communication.Email;

/// <summary>
/// Fake sender: logs the message instead of delivering it. Zero configuration — the default for dev
/// and tests. Swap for a real <see cref="IEmailSender"/> (e.g. MailKit) by registering it instead.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public const string ProviderName = "logging";

    public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[FAKE EMAIL] From: {From} | To: {To} | Cc: {Cc} | Subject: {Subject}\n{Body}",
            message.FromAddress ?? "(default sender)",
            string.Join(", ", message.To),
            message.Cc is { Count: > 0 } ? string.Join(", ", message.Cc) : "-",
            message.Subject, message.Body);

        var messageId = Guid.CreateVersion7().ToString("N");
        return Task.FromResult(new EmailDeliveryResult(ProviderName, messageId));
    }
}
