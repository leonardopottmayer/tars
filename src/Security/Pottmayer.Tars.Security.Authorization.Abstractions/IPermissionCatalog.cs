namespace Pottmayer.Tars.Security.Authorization.Abstractions;

/// <summary>
/// Every permission declared in code, validated at startup.
/// </summary>
public interface IPermissionCatalog
{
    /// <summary>All declared permissions, in declaration order.</summary>
    IReadOnlyList<PermissionDefinition> Definitions { get; }

    /// <summary>The permission with this key, or <c>null</c> when it was never declared.</summary>
    PermissionDefinition? Find(string key);
}
