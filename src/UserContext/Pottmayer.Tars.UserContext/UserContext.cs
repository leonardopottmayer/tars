using System.Security.Claims;
using Pottmayer.Tars.UserContext.Abstractions;

namespace Pottmayer.Tars.UserContext;

/// <summary>
/// Immutable claims-based user context. Built from a <see cref="ClaimsPrincipal"/> or a raw claims list.
/// </summary>
public sealed class UserContext : IUserContext
{
    private readonly IReadOnlyList<Claim> _claims;

    /// <summary>Represents an unauthenticated (anonymous) user with no claims.</summary>
    public static readonly IUserContext Anonymous = new UserContext([]);

    /// <summary>
    /// Creates a new context from the given claims.
    /// </summary>
    /// <param name="claims">The principal's claims; an empty list represents an anonymous user.</param>
    public UserContext(IReadOnlyList<Claim> claims)
    {
        _claims = claims ?? throw new ArgumentNullException(nameof(claims));
        IsAuthenticated = claims.Count > 0;
        UserId = GetClaim(ClaimTypes.NameIdentifier) ?? GetClaim("sub");
        Username = GetClaim(ClaimTypes.Name) ?? GetClaim("name");
        Email = GetClaim(ClaimTypes.Email) ?? GetClaim("email");
        Roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    }

    /// <inheritdoc />
    public bool IsAuthenticated { get; }

    /// <inheritdoc />
    public string? UserId { get; }

    /// <inheritdoc />
    public string? Username { get; }

    /// <inheritdoc />
    public string? Email { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Roles { get; }

    /// <inheritdoc />
    public IReadOnlyList<Claim> Claims => _claims;

    /// <inheritdoc />
    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetClaim(string claimType) =>
        _claims.FirstOrDefault(c => c.Type == claimType)?.Value;
}
