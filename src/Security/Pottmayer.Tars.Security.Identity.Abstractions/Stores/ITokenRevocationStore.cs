namespace Pottmayer.Tars.Security.Identity.Abstractions.Stores;

/// <summary>
/// Store for stateful token revocation (JTI blacklist or session version).
/// Revoked tokens must result in 401 Unauthorized.
/// </summary>
public interface ITokenRevocationStore
{
    /// <summary>
    /// Records that the given JWT ID (jti) is revoked.
    /// </summary>
    ValueTask RevokeJtiAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the given JWT ID is revoked.
    /// </summary>
    ValueTask<bool> IsJtiRevokedAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current session version for the subject (for SessionVersion strategy).
    /// </summary>
    ValueTask<long> GetSessionVersionAsync(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the session version for the subject (invalidates all tokens with older sv).
    /// </summary>
    ValueTask<long> IncrementSessionVersionAsync(string subject, CancellationToken cancellationToken = default);
}
