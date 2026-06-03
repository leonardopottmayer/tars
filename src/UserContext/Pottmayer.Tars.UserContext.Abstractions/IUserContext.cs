using System.Security.Claims;

namespace Pottmayer.Tars.UserContext.Abstractions;

/// <summary>
/// Claims-based context for the currently authenticated user.
/// Host-agnostic: usable in ASP.NET Core, workers, Blazor, and unit tests.
/// </summary>
public interface IUserContext
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<Claim> Claims { get; }
    bool IsInRole(string role);
    string? GetClaim(string claimType);
}
