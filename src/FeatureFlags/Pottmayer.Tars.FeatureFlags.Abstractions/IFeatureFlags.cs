namespace Pottmayer.Tars.FeatureFlags.Abstractions;

/// <summary>
/// Answers whether a feature is on in the system, for every user alike.
/// </summary>
public interface IFeatureFlags
{
    /// <summary>
    /// Whether the flag is on: its override (or default) is on and so is every ancestor's.
    /// </summary>
    /// <exception cref="InvalidOperationException">The key was never declared.</exception>
    ValueTask<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>The state of every declared flag, in declaration order.</summary>
    ValueTask<IReadOnlyList<FeatureFlagState>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the cached overrides, so the next call reads the store again. Call it after an
    /// override is written and committed.
    /// </summary>
    void Invalidate();
}
