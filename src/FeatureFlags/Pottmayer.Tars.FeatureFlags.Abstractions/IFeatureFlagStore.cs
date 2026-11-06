namespace Pottmayer.Tars.FeatureFlags.Abstractions;

/// <summary>
/// Reads the overrides an administrator stored. Writing them is the application's job (it usually
/// audits the change in the same unit of work); call <see cref="IFeatureFlags.Invalidate"/> after
/// the write commits.
/// </summary>
/// <remarks>
/// Resolved from a fresh DI scope on every load, so a scoped implementation (an EF Core
/// <c>DbContext</c>, for example) is fine. Overrides for keys that are not declared are ignored.
/// </remarks>
public interface IFeatureFlagStore
{
    /// <summary>The stored overrides, by flag key.</summary>
    Task<IReadOnlyDictionary<string, bool>> GetOverridesAsync(CancellationToken cancellationToken = default);
}
