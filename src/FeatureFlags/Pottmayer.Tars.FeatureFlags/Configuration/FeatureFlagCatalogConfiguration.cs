using Pottmayer.Tars.FeatureFlags.Abstractions;

namespace Pottmayer.Tars.FeatureFlags.Configuration;

/// <summary>
/// Collects the flags each module declares. Every call to
/// <see cref="DI.FeatureFlagServicesDI.AddTarsFeatureFlagDefinitions"/> adds to the same instance,
/// and the whole set is validated once at startup.
/// </summary>
public sealed class FeatureFlagCatalogConfiguration
{
    internal List<FeatureFlagDefinition> Definitions { get; } = [];

    /// <summary>Declares a flag.</summary>
    /// <param name="key">Unique key, for example <c>finances.import</c>.</param>
    /// <param name="defaultOn">The state used while no override is stored.</param>
    /// <param name="parent">Key of the parent flag; the flag is off whenever the parent is off.</param>
    public FeatureFlagCatalogConfiguration Add(string key, bool defaultOn = true, string? parent = null)
    {
        Definitions.Add(new FeatureFlagDefinition(key, defaultOn, parent));
        return this;
    }
}
