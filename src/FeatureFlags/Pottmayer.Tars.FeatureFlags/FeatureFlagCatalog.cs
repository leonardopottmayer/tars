using Microsoft.Extensions.Options;
using Pottmayer.Tars.FeatureFlags.Abstractions;
using Pottmayer.Tars.FeatureFlags.Configuration;

namespace Pottmayer.Tars.FeatureFlags;

internal sealed class FeatureFlagCatalog : IFeatureFlagCatalog
{
    private readonly Dictionary<string, FeatureFlagDefinition> _byKey;

    public FeatureFlagCatalog(IOptions<FeatureFlagCatalogConfiguration> configuration)
    {
        Definitions = configuration.Value.Definitions.ToArray();
        _byKey = Definitions.ToDictionary(d => d.Key, StringComparer.Ordinal);
    }

    public IReadOnlyList<FeatureFlagDefinition> Definitions { get; }

    public FeatureFlagDefinition? Find(string key) => _byKey.GetValueOrDefault(key);
}
