using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Security.Authorization.Abstractions;

namespace Pottmayer.Tars.Security.Authorization;

/// <summary>
/// Expands a subject's grants against the catalog into a flat set of keys and caches it per
/// subject until invalidated.
/// </summary>
/// <remarks>
/// ponytail: one cache entry per subject seen, never evicted, and local to this process; move to an
/// expiring or distributed cache if subjects run into the hundreds of thousands or the app scales out.
/// </remarks>
internal sealed class PermissionChecker(IPermissionCatalog catalog, IServiceScopeFactory scopeFactory) : IPermissionChecker
{
    private readonly ConcurrentDictionary<string, IReadOnlySet<string>> _cache = new(StringComparer.Ordinal);
    private int _version;

    public async ValueTask<bool> HasAsync(string subjectId, string permission, CancellationToken cancellationToken = default)
    {
        if (catalog.Find(permission) is null)
            throw new InvalidOperationException(
                $"Permission '{permission}' is not declared. Declare it with AddTarsPermissionDefinitions.");

        return (await GetEffectiveAsync(subjectId, cancellationToken)).Contains(permission);
    }

    public async ValueTask<IReadOnlySet<string>> GetEffectiveAsync(string subjectId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(subjectId, out var cached))
            return cached;

        var version = Volatile.Read(ref _version);

        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IAuthorizationStore>();
        var grants = await store.GetGrantsAsync(subjectId, cancellationToken);
        var effective = Expand(grants);

        // An invalidation during the load means the grants read may already be stale: use them for
        // this call, but do not cache them.
        if (version == Volatile.Read(ref _version))
            _cache[subjectId] = effective;

        return effective;
    }

    public void Invalidate(string subjectId)
    {
        Interlocked.Increment(ref _version);
        _cache.TryRemove(subjectId, out _);
    }

    public void InvalidateAll()
    {
        Interlocked.Increment(ref _version);
        _cache.Clear();
    }

    private IReadOnlySet<string> Expand(AuthorizationGrants grants)
    {
        var granted = grants.RolePatterns.Concat(grants.Allowed).ToArray();

        return catalog.Definitions
            .Select(d => d.Key)
            .Where(key => granted.Any(p => PermissionPatterns.Matches(p, key))
                       && !grants.Denied.Any(p => PermissionPatterns.Matches(p, key)))
            .ToHashSet(StringComparer.Ordinal);
    }
}
