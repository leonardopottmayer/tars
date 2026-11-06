namespace Pottmayer.Tars.Security.Authorization.Abstractions;

/// <summary>
/// What a subject was granted, as permission patterns. A pattern is an exact key, a prefix wildcard
/// (<c>finances.*</c>, <c>finances.cards.*</c>) or <c>*</c>.
/// </summary>
/// <param name="RolePatterns">Patterns of every role the subject holds.</param>
/// <param name="Allowed">Patterns allowed directly to the subject.</param>
/// <param name="Denied">Patterns denied directly to the subject; a denial beats any grant.</param>
public sealed record AuthorizationGrants(
    IReadOnlyList<string> RolePatterns,
    IReadOnlyList<string> Allowed,
    IReadOnlyList<string> Denied)
{
    /// <summary>A subject with no grants at all.</summary>
    public static AuthorizationGrants None { get; } = new([], [], []);
}

/// <summary>
/// Reads a subject's grants. Managing roles and grants is the application's job; call
/// <see cref="IPermissionChecker.Invalidate"/> or <see cref="IPermissionChecker.InvalidateAll"/> after a
/// change commits.
/// </summary>
/// <remarks>
/// Resolved from a fresh DI scope on every load, so a scoped implementation (an EF Core
/// <c>DbContext</c>, for example) is fine. Patterns that match no declared key are ignored.
/// </remarks>
public interface IAuthorizationStore
{
    /// <summary>The subject's grants, or <see cref="AuthorizationGrants.None"/> when it has none.</summary>
    Task<AuthorizationGrants> GetGrantsAsync(string subjectId, CancellationToken cancellationToken = default);
}
