namespace Pottmayer.Tars.Communication.Email.Abstractions;

/// <summary>
/// Transport-agnostic e-mail sender. Implemented by a logging fake or a real SMTP provider
/// (e.g. MailKit); callers depend only on this contract. Implementations throw on delivery
/// failure so the caller can decide how to retry.
/// </summary>
public interface IEmailSender
{
    Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
