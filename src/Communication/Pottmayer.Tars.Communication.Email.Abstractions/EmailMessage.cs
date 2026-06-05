namespace Pottmayer.Tars.Communication.Email.Abstractions;

/// <summary>
/// A single e-mail to deliver. <see cref="To"/> must hold at least one address; <see cref="Cc"/> is
/// optional. <see cref="FromAddress"/> / <see cref="FromName"/> are optional; when null the transport
/// falls back to its configured default sender.
/// </summary>
public sealed record EmailMessage(
    IReadOnlyList<string> To,
    string Subject,
    string Body,
    bool IsHtml = false,
    IReadOnlyList<string>? Cc = null,
    string? FromAddress = null,
    string? FromName = null);
