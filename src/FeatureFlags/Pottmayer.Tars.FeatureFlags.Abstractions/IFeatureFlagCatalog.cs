namespace Pottmayer.Tars.FeatureFlags.Abstractions;

/// <summary>
/// Every flag declared in code, validated at startup (unique keys, known parents, no cycles).
/// </summary>
public interface IFeatureFlagCatalog
{
    /// <summary>All declared flags, in declaration order.</summary>
    IReadOnlyList<FeatureFlagDefinition> Definitions { get; }

    /// <summary>The flag with this key, or <c>null</c> when it was never declared.</summary>
    FeatureFlagDefinition? Find(string key);
}
