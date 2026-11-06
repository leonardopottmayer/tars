namespace Pottmayer.Tars.Security.Authorization.Abstractions;

/// <summary>
/// The standard action segments of a permission key (<c>module.resource.action</c>). Constants, so a
/// module can build its own keys from them and use those in <see cref="RequiresPermissionAttribute"/>.
/// </summary>
/// <example>
/// <code>
/// public const string Delete = "finances.cards." + PermissionActions.Delete;
/// </code>
/// </example>
public static class PermissionActions
{
    /// <summary>See a resource. Every other action of a resource that declares it requires it.</summary>
    public const string Read = "read";

    /// <summary>Create a resource.</summary>
    public const string Create = "create";

    /// <summary>Change a resource.</summary>
    public const string Update = "update";

    /// <summary>Remove a resource.</summary>
    public const string Delete = "delete";
}
