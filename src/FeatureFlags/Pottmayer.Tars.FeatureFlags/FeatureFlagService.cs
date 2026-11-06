using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.FeatureFlags.Abstractions;

namespace Pottmayer.Tars.FeatureFlags;

/// <summary>
/// Resolves flags from the catalog defaults and the store's overrides, caching the overrides in
/// memory until <see cref="Invalidate"/>.
/// </summary>
/// <remarks>
/// ponytail: the cache lives in this process only; with several instances, an override reaches the
/// others only when they are invalidated too (add a pub/sub invalidation if that ever happens).
/// </remarks>
internal sealed class FeatureFlagService(IFeatureFlagCatalog catalog, IServiceScopeFactory scopeFactory) : IFeatureFlags
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile IReadOnlyDictionary<string, bool>? _overrides;
    private int _version;

    public async ValueTask<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default)
    {
        var definition = catalog.Find(key) ?? throw new InvalidOperationException(
            $"Feature flag '{key}' is not declared. Declare it with AddTarsFeatureFlagDefinitions.");

        return Resolve(definition, await GetOverridesAsync(cancellationToken));
    }

    public async ValueTask<IReadOnlyList<FeatureFlagState>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var overrides = await GetOverridesAsync(cancellationToken);

        return catalog.Definitions
            .Select(d => new FeatureFlagState(
                d,
                overrides.TryGetValue(d.Key, out var value) ? value : null,
                Resolve(d, overrides)))
            .ToArray();
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _version);
        _overrides = null;
    }

    private bool Resolve(FeatureFlagDefinition definition, IReadOnlyDictionary<string, bool> overrides)
    {
        // The catalog is validated at startup (known parents, no cycles), so this walk ends.
        for (var current = definition; current is not null;
             current = current.Parent is null ? null : catalog.Find(current.Parent))
        {
            var enabled = overrides.TryGetValue(current.Key, out var value) ? value : current.DefaultEnabled;
            if (!enabled)
                return false;
        }

        return true;
    }

    private async ValueTask<IReadOnlyDictionary<string, bool>> GetOverridesAsync(CancellationToken cancellationToken)
    {
        if (_overrides is { } cached)
            return cached;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_overrides is { } loadedMeanwhile)
                return loadedMeanwhile;

            var version = Volatile.Read(ref _version);

            await using var scope = scopeFactory.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IFeatureFlagStore>();
            var loaded = await store.GetOverridesAsync(cancellationToken);

            // An Invalidate() during the load means what was read may already be stale: use it for
            // this call, but do not cache it.
            if (version == Volatile.Read(ref _version))
                _overrides = loaded;

            return loaded;
        }
        finally
        {
            _gate.Release();
        }
    }
}
