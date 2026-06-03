namespace Pottmayer.Tars.Security.Identity.Abstractions.Stores;

/// <summary>
/// Store for magic link tokens (token -> payload with TTL). Framework generates token and stores; app sends link.
/// </summary>
public interface IMagicLinkTokenStore
{
    /// <summary>
    /// Stores a magic link token with the given payload and expiration.
    /// </summary>
    ValueTask StoreAsync(
        string token,
        IReadOnlyDictionary<string, object?> payload,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes the token: returns and removes payload if valid and not expired; otherwise null.
    /// </summary>
    ValueTask<IReadOnlyDictionary<string, object?>?> ConsumeAsync(
        string token,
        CancellationToken cancellationToken = default);
}
