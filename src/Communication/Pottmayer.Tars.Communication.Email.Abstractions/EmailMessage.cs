namespace Pottmayer.Tars.Communication.Email.Abstractions;

/// <summary>
/// A single e-mail to deliver. <see cref="To"/> must hold at least one address; <see cref="Cc"/> is
/// optional. <see cref="FromAddress"/> / <see cref="FromName"/> are optional; when null the transport
/// falls back to its configured default sender.
/// </summary>
/// <param name="To">Recipient addresses; must hold at least one entry.</param>
/// <param name="Subject">The message subject line.</param>
/// <param name="Body">The message body, plain text or HTML per <paramref name="IsHtml"/>.</param>
/// <param name="IsHtml">Whether <paramref name="Body"/> is HTML rather than plain text.</param>
/// <param name="Cc">Optional carbon-copy addresses.</param>
/// <param name="FromAddress">Optional sender address; when null the transport uses its configured default.</param>
/// <param name="FromName">Optional sender display name; when null the transport uses its configured default.</param>
public sealed record EmailMessage(
    IReadOnlyList<string> To,
    string Subject,
    string Body,
    bool IsHtml = false,
    IReadOnlyList<string>? Cc = null,
    string? FromAddress = null,
    string? FromName = null);
