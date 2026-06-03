using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Stores;

/// <summary>
/// Store for refresh tokens. Used for rotation, reuse detection, and revocation.
/// Implement in Core (InMemory) or by host (e.g. Redis) for multi-instance.
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>
    /// Stores a refresh token with the given id, secret hash, subject, claims, and expiration.
    /// The raw secret is never stored — only <paramref name="tokenHash"/>.
    /// </summary>
    ValueTask StoreAsync(
        string tokenId,
        string tokenHash,
        string subject,
        IReadOnlyList<ClaimData> claims,
        DateTimeOffset expiresAt,
        IReadOnlyDictionary<string, object?>? metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the payload and marks the token consumed only if it exists, is valid
    /// (not revoked, not expired) and <paramref name="tokenHash"/> matches the stored hash.
    /// Returns null otherwise. Used for rotation: invalidate old id after successful use.
    /// </summary>
    ValueTask<RefreshTokenPayload?> GetAndRemoveAsync(
        string tokenId,
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the payload without consuming it, only if the token is valid and
    /// <paramref name="tokenHash"/> matches the stored hash. Used when rotation is disabled.
    /// </summary>
    ValueTask<RefreshTokenPayload?> GetAsync(string tokenId, string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token by id (e.g. on sign-out).
    /// </summary>
    ValueTask RevokeAsync(string tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for the given subject (e.g. sign-out all devices).
    /// </summary>
    ValueTask RevokeAllForSubjectAsync(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// For reuse detection: returns the subject that owned this token id if it was already consumed.
    /// Returns null if not found or not applicable.
    /// </summary>
    ValueTask<string?> TryGetSubjectForReuseAsync(string tokenId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Payload stored with a refresh token.
/// </summary>
public sealed class RefreshTokenPayload
{
    public required string Subject { get; init; }
    public required IReadOnlyList<ClaimData> Claims { get; init; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}
