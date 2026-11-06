using Microsoft.Extensions.Options;

namespace Pottmayer.Tars.FeatureFlags.Configuration;

/// <summary>
/// Rejects a catalog with blank or duplicate keys, unknown parents or parent cycles, so the host
/// fails at startup instead of on the first check.
/// </summary>
internal sealed class FeatureFlagCatalogValidation : IValidateOptions<FeatureFlagCatalogConfiguration>
{
    public ValidateOptionsResult Validate(string? name, FeatureFlagCatalogConfiguration options)
    {
        var errors = new List<string>();
        var parents = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var definition in options.Definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Key))
                errors.Add("A feature flag was declared with a blank key.");
            else if (!parents.TryAdd(definition.Key, definition.Parent))
                errors.Add($"Feature flag '{definition.Key}' is declared more than once.");
        }

        foreach (var (key, parent) in parents)
        {
            if (parent is not null && !parents.ContainsKey(parent))
                errors.Add($"Feature flag '{key}' has parent '{parent}', which is not declared. Declare it with AddTarsFeatureFlagDefinitions.");
        }

        foreach (var key in parents.Keys)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { key };
            for (var current = parents[key]; current is not null; current = parents.GetValueOrDefault(current))
            {
                if (!seen.Add(current))
                {
                    errors.Add($"Feature flag '{key}' is part of a parent cycle.");
                    break;
                }
            }
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
