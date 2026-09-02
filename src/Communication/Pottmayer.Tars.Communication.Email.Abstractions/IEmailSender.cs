namespace Pottmayer.Tars.Communication.Email.Abstractions;

/// <summary>
/// Transport-agnostic e-mail sender. Implemented by a logging fake or a real SMTP provider
/// (e.g. MailKit); callers depend only on this contract. Implementations throw on delivery
/// failure so the caller can decide how to retry.
/// </summary>
public interface IEmailSender
{
    /// <summary>Delivers <paramref name="message"/> through the underlying transport.</summary>
    /// <param name="message">The e-mail to send.</param>
    /// <param name="cancellationToken">Token used to cancel the delivery.</param>
    /// <returns>The delivery outcome, including the accepting provider and its message id, if any.</returns>
    Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
