using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.FeatureFlags.Abstractions;
using Pottmayer.Tars.Security.Authorization.Abstractions;

namespace Pottmayer.Tars.Security.Authorization.Configuration;

/// <summary>
/// Rejects a catalog with invalid segments, duplicate keys, unknown features, or a
/// <see cref="RequiresPermissionAttribute"/> naming an undeclared key, so the host fails at startup
/// instead of on the first request.
/// </summary>
internal sealed class PermissionCatalogValidation(IServiceProvider services) : IValidateOptions<PermissionCatalogConfiguration>
{
    private const BindingFlags AllMethods =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public ValidateOptionsResult Validate(string? name, PermissionCatalogConfiguration options)
    {
        var errors = new List<string>();
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var d in options.Definitions)
        {
            if (!IsSegment(d.Module) || !IsSegment(d.Resource) || !IsSegment(d.Action))
                errors.Add($"Permission '{d.Key}' has a blank segment or one containing '.' or '*'.");
            else if (!keys.Add(d.Key))
                errors.Add($"Permission '{d.Key}' is declared more than once.");
        }

        var features = options.Definitions.Where(d => d.Feature is not null).Select(d => d.Feature!).Distinct().ToArray();
        if (features.Length > 0)
        {
            var flags = services.GetService<IFeatureFlagCatalog>();
            if (flags is null)
                errors.Add("Permissions are gated by feature flags, but no IFeatureFlagCatalog is registered. Call AddTarsFeatureFlagCatalog.");
            else
                errors.AddRange(features
                    .Where(f => flags.Find(f) is null)
                    .Select(f => $"Feature flag '{f}' gates permissions but is not declared. Declare it with AddTarsFeatureFlagDefinitions."));
        }

        foreach (var (target, permission) in options.UsageAssemblies.Distinct().SelectMany(Usages))
        {
            if (!keys.Contains(permission))
                errors.Add($"{target} requires permission '{permission}', which is not declared. Declare it with AddTarsPermissionDefinitions.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static bool IsSegment(string segment)
        => !string.IsNullOrWhiteSpace(segment) && !segment.Contains('.') && !segment.Contains('*');

    private static IEnumerable<(string Target, string Permission)> Usages(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            foreach (var attribute in type.GetCustomAttributes<RequiresPermissionAttribute>(inherit: false))
                yield return (type.FullName ?? type.Name, attribute.Permission);

            foreach (var method in type.GetMethods(AllMethods))
                foreach (var attribute in method.GetCustomAttributes<RequiresPermissionAttribute>(inherit: false))
                    yield return ($"{type.FullName}.{method.Name}", attribute.Permission);
        }
    }
}
