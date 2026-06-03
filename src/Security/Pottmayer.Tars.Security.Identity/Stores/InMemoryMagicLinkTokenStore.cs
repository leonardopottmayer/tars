using System.Collections.Concurrent;
using Pottmayer.Tars.Security.Identity.Abstractions.Stores;

namespace Pottmayer.Tars.Security.Identity.Stores;

/// <summary>
/// In-memory magic link token store. For development and single-instance only.
/// </summary>
public sealed class InMemoryMagicLinkTokenStore : IMagicLinkTokenStore
{
    private readonly ConcurrentDictionary<string, (IReadOnlyDictionary<string, object?> Payload, DateTimeOffset ExpiresAt)> _tokens = new();

    public ValueTask StoreAsync(
        string token,
        IReadOnlyDictionary<string, object?> payload,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        _tokens[token] = (payload, expiresAt);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyDictionary<string, object?>?> ConsumeAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryRemove(token, out var entry))
            return ValueTask.FromResult<IReadOnlyDictionary<string, object?>?>(null);

        if (entry.ExpiresAt < DateTimeOffset.UtcNow)
            return ValueTask.FromResult<IReadOnlyDictionary<string, object?>?>(null);

        return ValueTask.FromResult<IReadOnlyDictionary<string, object?>?>(entry.Payload);
    }
}
