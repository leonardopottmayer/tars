using System.Collections.Concurrent;
using Pottmayer.Tars.Security.Identity.Abstractions.Stores;

namespace Pottmayer.Tars.Security.Identity.Stores;

/// <summary>
/// In-memory token revocation store (JTI blacklist and session version). For development and single-instance only.
/// </summary>
public sealed class InMemoryTokenRevocationStore : ITokenRevocationStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _revokedJtis = new();
    private readonly ConcurrentDictionary<string, long> _sessionVersions = new();

    public ValueTask RevokeJtiAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        _revokedJtis[jti] = expiresAt;
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> IsJtiRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (!_revokedJtis.TryGetValue(jti, out var expiresAt))
            return ValueTask.FromResult(false);

        if (expiresAt < DateTimeOffset.UtcNow)
        {
            _revokedJtis.TryRemove(jti, out _);
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }

    public ValueTask<long> GetSessionVersionAsync(string subject, CancellationToken cancellationToken = default)
    {
        var v = _sessionVersions.GetValueOrDefault(subject, 0L);
        return ValueTask.FromResult(v);
    }

    public ValueTask<long> IncrementSessionVersionAsync(string subject, CancellationToken cancellationToken = default)
    {
        var v = _sessionVersions.AddOrUpdate(subject, 1, (_, n) => n + 1);
        return ValueTask.FromResult(v);
    }
}
