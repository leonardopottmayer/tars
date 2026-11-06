using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Authorization.Abstractions;
using Pottmayer.Tars.Security.Authorization.Configuration;

namespace Pottmayer.Tars.Security.Authorization;

internal sealed class PermissionCatalog : IPermissionCatalog
{
    private readonly Dictionary<string, PermissionDefinition> _byKey;

    public PermissionCatalog(IOptions<PermissionCatalogConfiguration> configuration)
    {
        var declared = configuration.Value.Definitions;
        var keys = declared.Select(d => d.Key).ToHashSet(StringComparer.Ordinal);

        // Every action of a resource that declares "read" requires it; worked out here because
        // "read" may be declared after the actions that depend on it.
        Definitions = declared
            .Select(d =>
            {
                var read = $"{d.Module}.{d.Resource}.read";
                return d.Action != "read" && keys.Contains(read) ? d with { Requires = [read] } : d;
            })
            .ToArray();

        _byKey = Definitions.ToDictionary(d => d.Key, StringComparer.Ordinal);
    }

    public IReadOnlyList<PermissionDefinition> Definitions { get; }

    public PermissionDefinition? Find(string key) => _byKey.GetValueOrDefault(key);
}
