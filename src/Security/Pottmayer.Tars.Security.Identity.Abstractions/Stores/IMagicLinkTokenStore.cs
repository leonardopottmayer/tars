namespace Pottmayer.Tars.Security.Identity.Abstractions.Stores;

/// <summary>
/// Store for magic link tokens (token hash -> payload with TTL). Framework generates the token and stores only its
/// SHA-256 hash, so whoever reads the store cannot sign in with it; the app sends the link with the raw token.
/// </summary>
public interface IMagicLinkTokenStore
{
    /// <summary>
    /// Stores a magic link token hash with the given payload and expiration.
    /// </summary>
    ValueTask StoreAsync(
        string tokenHash,
        IReadOnlyDictionary<string, object?> payload,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes the token hash: returns and removes payload if valid and not expired; otherwise null.
    /// </summary>
    ValueTask<IReadOnlyDictionary<string, object?>?> ConsumeAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);
}
