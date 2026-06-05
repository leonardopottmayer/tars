using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Pottmayer.Tars.Communication.Email.Abstractions;
using Pottmayer.Tars.Communication.Email.MailKit.Options;

namespace Pottmayer.Tars.Communication.Email.MailKit;

/// <summary>
/// Delivers e-mail over SMTP via MailKit. Stateless: opens a fresh connection per send, so it is safe
/// as a singleton. Throws on failure so the caller can retry.
/// </summary>
public sealed class MailKitEmailSender(IOptions<MailKitEmailOptions> options) : IEmailSender
{
    public const string ProviderName = "mailkit";

    public async Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        var mime = BuildMimeMessage(message, settings);

        using var client = new SmtpClient();
        await client.ConnectAsync(
            settings.Host,
            settings.Port,
            settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
            cancellationToken);

        if (!string.IsNullOrEmpty(settings.Username))
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);

        var response = await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        return new EmailDeliveryResult(ProviderName, response);
    }

    /// <summary>
    /// Maps an <see cref="EmailMessage"/> onto a MIME message. Sender falls back to the configured
    /// default when the message sets none; the body part is <c>text/html</c> or <c>text/plain</c>
    /// per <see cref="EmailMessage.IsHtml"/>.
    /// </summary>
    internal static MimeMessage BuildMimeMessage(EmailMessage message, MailKitEmailOptions settings)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(
            message.FromName ?? settings.FromName,
            message.FromAddress ?? settings.FromAddress));
        foreach (var to in message.To)
            mime.To.Add(MailboxAddress.Parse(to));

        if (message.Cc is { Count: > 0 })
            foreach (var cc in message.Cc)
                mime.Cc.Add(MailboxAddress.Parse(cc));

        mime.Subject = message.Subject;
        mime.Body = new TextPart(message.IsHtml ? "html" : "plain") { Text = message.Body };

        return mime;
    }
}
