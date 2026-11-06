namespace Pottmayer.Tars.Security.Authorization.Abstractions;

/// <summary>
/// Marks a mediator request, a controller or an action as requiring a permission. Several
/// attributes must all be held. The permission's feature flag, if any, must be on first.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequiresPermissionAttribute(string permission) : Attribute
{
    /// <summary>The permission key, for example <c>finances.cards.delete</c>.</summary>
    public string Permission { get; } = permission;
}
