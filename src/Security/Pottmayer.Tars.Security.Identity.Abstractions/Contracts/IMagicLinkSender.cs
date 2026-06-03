namespace Pottmayer.Tars.Security.Identity.Abstractions.Contracts;

/// <summary>
/// Implemented by the host to send the magic link (e.g. email, SMS).
/// The framework generates the token and calls this to deliver it.
/// </summary>
public interface IMagicLinkSender
{
    /// <summary>
    /// Sends the magic link to the given target.
    /// </summary>
    /// <param name="target">Target identifier (e.g. email address).</param>
    /// <param name="linkUrl">The full URL to use as the magic link (token is embedded).</param>
    /// <param name="expiresAt">When the link expires.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SendAsync(
        string target,
        string linkUrl,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}
