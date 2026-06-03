using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Abstractions.Stores;

namespace Pottmayer.Tars.Security.Identity.Stores;

/// <summary>
/// In-memory refresh token store. For development and single-instance only.
/// </summary>
public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, StoredRefreshToken> _tokens = new();
    private readonly ConcurrentDictionary<string, string> _consumedForReuse = new();

    public ValueTask StoreAsync(
        string tokenId,
        string tokenHash,
        string subject,
        IReadOnlyList<ClaimData> claims,
        DateTimeOffset expiresAt,
        IReadOnlyDictionary<string, object?>? metadata,
        CancellationToken cancellationToken = default)
    {
        _tokens[tokenId] = new StoredRefreshToken(tokenHash, subject, claims, expiresAt, metadata);
        return ValueTask.CompletedTask;
    }

    public ValueTask<RefreshTokenPayload?> GetAndRemoveAsync(string tokenId, string tokenHash, CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryGetValue(tokenId, out var stored))
            return ValueTask.FromResult<RefreshTokenPayload?>(null);

        if (stored.ExpiresAt < DateTimeOffset.UtcNow || !HashesMatch(stored.TokenHash, tokenHash))
            return ValueTask.FromResult<RefreshTokenPayload?>(null);

        _tokens.TryRemove(tokenId, out _);
        _consumedForReuse[tokenId] = stored.Subject;
        return ValueTask.FromResult<RefreshTokenPayload?>(new RefreshTokenPayload
        {
            Subject = stored.Subject,
            Claims = stored.Claims,
            Metadata = stored.Metadata
        });
    }

    public ValueTask<RefreshTokenPayload?> GetAsync(string tokenId, string tokenHash, CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryGetValue(tokenId, out var stored))
            return ValueTask.FromResult<RefreshTokenPayload?>(null);

        if (stored.ExpiresAt < DateTimeOffset.UtcNow || !HashesMatch(stored.TokenHash, tokenHash))
            return ValueTask.FromResult<RefreshTokenPayload?>(null);

        return ValueTask.FromResult<RefreshTokenPayload?>(new RefreshTokenPayload
        {
            Subject = stored.Subject,
            Claims = stored.Claims,
            Metadata = stored.Metadata
        });
    }

    public ValueTask RevokeAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        _tokens.TryRemove(tokenId, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask RevokeAllForSubjectAsync(string subject, CancellationToken cancellationToken = default)
    {
        foreach (var kv in _tokens.Where(kv => kv.Value.Subject == subject).ToList())
            _tokens.TryRemove(kv.Key, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask<string?> TryGetSubjectForReuseAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        _consumedForReuse.TryGetValue(tokenId, out var subject);
        return ValueTask.FromResult<string?>(subject);
    }

    private static bool HashesMatch(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private sealed record StoredRefreshToken(
        string TokenHash,
        string Subject,
        IReadOnlyList<ClaimData> Claims,
        DateTimeOffset ExpiresAt,
        IReadOnlyDictionary<string, object?>? Metadata);
}
