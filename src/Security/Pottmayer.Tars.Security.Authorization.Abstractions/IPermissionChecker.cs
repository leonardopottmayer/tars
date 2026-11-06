namespace Pottmayer.Tars.Security.Authorization.Abstractions;

/// <summary>
/// Answers whether a subject holds a permission. Feature flags are not considered here; the
/// authorization gate checks them first.
/// </summary>
public interface IPermissionChecker
{
    /// <summary>Whether the subject holds the permission.</summary>
    /// <exception cref="InvalidOperationException">The key was never declared.</exception>
    ValueTask<bool> HasAsync(string subjectId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// The subject's effective permissions: role patterns and direct allows, minus direct denials,
    /// expanded against the catalog.
    /// </summary>
    ValueTask<IReadOnlySet<string>> GetEffectiveAsync(string subjectId, CancellationToken cancellationToken = default);

    /// <summary>Drops the cached permissions of one subject. Call it after their grants change.</summary>
    void Invalidate(string subjectId);

    /// <summary>Drops every cached subject. Call it after a role changes.</summary>
    void InvalidateAll();
}
