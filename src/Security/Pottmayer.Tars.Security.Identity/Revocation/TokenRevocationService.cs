using Pottmayer.Tars.Security.Identity.Abstractions.Contracts;
using Pottmayer.Tars.Security.Identity.Abstractions.Stores;

namespace Pottmayer.Tars.Security.Identity.Revocation;

/// <summary>
/// Delegates revocation to the configured store (e.g. InMemory, Redis).
/// </summary>
public sealed class TokenRevocationService : ITokenRevocationService
{
    private readonly ITokenRevocationStore _store;

    public TokenRevocationService(ITokenRevocationStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ValueTask RevokeJtiAsync(string jti, DateTimeOffset tokenExpiresAt, CancellationToken cancellationToken = default)
        => _store.RevokeJtiAsync(jti, tokenExpiresAt, cancellationToken);

    public ValueTask<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
        => _store.IsJtiRevokedAsync(jti, cancellationToken);

    public ValueTask<long> GetSessionVersionAsync(string subject, CancellationToken cancellationToken = default)
        => _store.GetSessionVersionAsync(subject, cancellationToken);

    public ValueTask<long> IncrementSessionVersionAsync(string subject, CancellationToken cancellationToken = default)
        => _store.IncrementSessionVersionAsync(subject, cancellationToken);
}
