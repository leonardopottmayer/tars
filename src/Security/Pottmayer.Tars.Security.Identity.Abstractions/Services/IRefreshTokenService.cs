using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Services;

/// <summary>
/// Issues, consumes, and revokes refresh tokens with rotation and reuse detection.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Issues a new refresh token for the given subject.
    /// </summary>
    /// <param name="subject">The user identifier (subject) the token is issued for.</param>
    /// <param name="claims">The claims to associate with the token.</param>
    /// <param name="metadata">Optional metadata to store alongside the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The issued token and its metadata.</returns>
    ValueTask<RefreshTokenIssueResult> IssueAsync(
        string subject,
        IReadOnlyList<ClaimData> claims,
        IReadOnlyDictionary<string, object?>? metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes a refresh token, applying rotation and reuse-detection rules.
    /// </summary>
    /// <param name="opaqueToken">The opaque refresh token presented by the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The consume result, or null if the token is invalid, expired, or already revoked.</returns>
    ValueTask<RefreshTokenConsumeResult?> ConsumeAsync(string opaqueToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token, e.g. on sign-out.
    /// </summary>
    /// <param name="opaqueToken">The opaque refresh token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask RevokeAsync(string opaqueToken, CancellationToken cancellationToken = default);
}
