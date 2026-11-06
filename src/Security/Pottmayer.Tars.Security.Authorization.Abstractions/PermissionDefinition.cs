namespace Pottmayer.Tars.Security.Authorization.Abstractions;

/// <summary>
/// A permission as declared in code: one action on one resource of one module.
/// </summary>
/// <param name="Key">Full key, <c>module.resource.action</c> (for example <c>finances.cards.delete</c>).</param>
/// <param name="Module">The module segment.</param>
/// <param name="Resource">The resource segment.</param>
/// <param name="Action">The action segment.</param>
/// <param name="Feature">Feature flag that must be on before the permission is checked, if any.</param>
/// <param name="Requires">Keys this permission depends on (every action of a resource requires its <c>read</c>).</param>
public sealed record PermissionDefinition(
    string Key,
    string Module,
    string Resource,
    string Action,
    string? Feature,
    IReadOnlyList<string> Requires);
