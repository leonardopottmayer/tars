using System.Reflection;
using Pottmayer.Tars.Security.Authorization.Abstractions;

namespace Pottmayer.Tars.Security.Authorization.Configuration;

/// <summary>
/// Collects the permissions each module declares. Every call to
/// <see cref="DI.AuthorizationServicesDI.AddTarsPermissionDefinitions"/> adds to the same instance, and
/// the whole set is validated once at startup.
/// </summary>
/// <example>
/// <code>
/// permissions.Module("finances", feature: "finances")
///     .Resource("cards").Crud()                      // finances.cards.read/create/update/delete
///     .Resource("imports", feature: "finances.import").Actions("run", "read")
///     .Resource("transactions").Crud().Actions("approve");
/// </code>
/// </example>
public sealed class PermissionCatalogConfiguration
{
    internal List<PermissionDefinition> Definitions { get; } = [];
    internal List<Assembly> UsageAssemblies { get; } = [];

    /// <summary>Starts declaring the resources of a module.</summary>
    /// <param name="key">Module segment of the keys, for example <c>finances</c>.</param>
    /// <param name="feature">Feature flag that gates every permission of the module.</param>
    public PermissionModuleBuilder Module(string key, string? feature = null) => new(this, key, feature);

    /// <summary>
    /// At startup, checks that every <see cref="RequiresPermissionAttribute"/> found on the types of
    /// this assembly (and on their methods) names a declared permission.
    /// </summary>
    public PermissionCatalogConfiguration CheckUsagesIn(Assembly assembly)
    {
        UsageAssemblies.Add(assembly);
        return this;
    }
}

/// <summary>Declares the resources of one module.</summary>
public sealed class PermissionModuleBuilder
{
    private readonly PermissionCatalogConfiguration _catalog;
    private readonly string _module;
    private readonly string? _feature;

    internal PermissionModuleBuilder(PermissionCatalogConfiguration catalog, string module, string? feature)
    {
        _catalog = catalog;
        _module = module;
        _feature = feature;
    }

    /// <summary>Starts declaring the actions of a resource.</summary>
    /// <param name="key">Resource segment of the keys, for example <c>cards</c>.</param>
    /// <param name="feature">Feature flag for this resource; overrides the module's.</param>
    public PermissionResourceBuilder Resource(string key, string? feature = null)
        => new(this, key, feature ?? _feature);

    internal void Add(string resource, string action, string? feature)
        => _catalog.Definitions.Add(new PermissionDefinition(
            $"{_module}.{resource}.{action}", _module, resource, action, feature, []));
}

/// <summary>Declares the actions of one resource.</summary>
public sealed class PermissionResourceBuilder
{
    private readonly PermissionModuleBuilder _module;
    private readonly string _resource;
    private readonly string? _feature;

    internal PermissionResourceBuilder(PermissionModuleBuilder module, string resource, string? feature)
    {
        _module = module;
        _resource = resource;
        _feature = feature;
    }

    /// <summary>Declares <c>read</c>, <c>create</c>, <c>update</c> and <c>delete</c>.</summary>
    public PermissionResourceBuilder Crud()
        => Actions(PermissionActions.Read, PermissionActions.Create, PermissionActions.Update, PermissionActions.Delete);

    /// <summary>Declares <c>read</c>.</summary>
    public PermissionResourceBuilder Read() => Actions(PermissionActions.Read);

    /// <summary>Declares <c>create</c>.</summary>
    public PermissionResourceBuilder Create() => Actions(PermissionActions.Create);

    /// <summary>Declares <c>update</c>.</summary>
    public PermissionResourceBuilder Update() => Actions(PermissionActions.Update);

    /// <summary>Declares <c>delete</c>.</summary>
    public PermissionResourceBuilder Delete() => Actions(PermissionActions.Delete);

    /// <summary>Declares actions. Every action of a resource that has <c>read</c> requires it.</summary>
    public PermissionResourceBuilder Actions(params string[] actions)
    {
        foreach (var action in actions)
            _module.Add(_resource, action, _feature);
        return this;
    }

    /// <summary>Moves on to another resource of the same module.</summary>
    public PermissionResourceBuilder Resource(string key, string? feature = null) => _module.Resource(key, feature);
}
