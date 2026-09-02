using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Services;

/// <summary>
/// Generates and consumes magic link tokens.
/// </summary>
public interface IMagicLinkTokenService
{
    /// <summary>
    /// Issues a new magic link token carrying the given payload.
    /// </summary>
    /// <param name="payload">Application-defined data to associate with the token (e.g. the target user identifier).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The issued token and its expiration.</returns>
    ValueTask<MagicLinkIssueResult> IssueAsync(
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes a magic link token, invalidating it for reuse.
    /// </summary>
    /// <param name="token">The token to consume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payload associated with the token, or null if the token is invalid, expired, or already consumed.</returns>
    ValueTask<IReadOnlyDictionary<string, object?>?> ConsumeAsync(string token, CancellationToken cancellationToken = default);
}
