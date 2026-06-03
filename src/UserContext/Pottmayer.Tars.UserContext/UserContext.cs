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

    public UserContext(IReadOnlyList<Claim> claims)
    {
        _claims = claims ?? throw new ArgumentNullException(nameof(claims));
        IsAuthenticated = claims.Count > 0;
        UserId = GetClaim(ClaimTypes.NameIdentifier) ?? GetClaim("sub");
        Username = GetClaim(ClaimTypes.Name) ?? GetClaim("name");
        Email = GetClaim(ClaimTypes.Email) ?? GetClaim("email");
        Roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    }

    public bool IsAuthenticated { get; }
    public string? UserId { get; }
    public string? Username { get; }
    public string? Email { get; }
    public IReadOnlyList<string> Roles { get; }
    public IReadOnlyList<Claim> Claims => _claims;

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public string? GetClaim(string claimType) =>
        _claims.FirstOrDefault(c => c.Type == claimType)?.Value;
}
