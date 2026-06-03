namespace Pottmayer.Tars.Security.Identity.Abstractions.Contracts;

/// <summary>
/// Service for stateful token revocation (JTI blacklist and/or session version).
/// Revoked tokens must result in 401 Unauthorized, not 503.
/// </summary>
public interface ITokenRevocationService
{
    /// <summary>
    /// Records that the given JWT ID is revoked until its expiration.
    /// </summary>
    ValueTask RevokeJtiAsync(string jti, DateTimeOffset tokenExpiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the JWT ID is revoked.
    /// </summary>
    ValueTask<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current session version for the subject (for sv claim).
    /// </summary>
    ValueTask<long> GetSessionVersionAsync(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the session version for the subject (invalidates all tokens with older sv).
    /// </summary>
    ValueTask<long> IncrementSessionVersionAsync(string subject, CancellationToken cancellationToken = default);
}
