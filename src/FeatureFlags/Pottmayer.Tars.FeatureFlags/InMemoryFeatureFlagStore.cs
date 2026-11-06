using System.Collections.Concurrent;
using Pottmayer.Tars.FeatureFlags.Abstractions;

namespace Pottmayer.Tars.FeatureFlags;

/// <summary>
/// Keeps overrides in memory, for tests and for applications that switch flags only in code.
/// Overrides are lost on restart.
/// </summary>
public sealed class InMemoryFeatureFlagStore : IFeatureFlagStore
{
    private readonly ConcurrentDictionary<string, bool> _overrides = new(StringComparer.Ordinal);

    /// <summary>Stores an override, or removes it when <paramref name="enabled"/> is <c>null</c>.</summary>
    /// <remarks>Call <see cref="IFeatureFlags.Invalidate"/> afterwards so the change is seen.</remarks>
    public void Set(string key, bool? enabled)
    {
        if (enabled is { } value)
            _overrides[key] = value;
        else
            _overrides.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, bool>> GetOverridesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<string, bool>>(
            new Dictionary<string, bool>(_overrides, StringComparer.Ordinal));
}
