using System.Security.Claims;

namespace Pottmayer.Tars.UserContext.Abstractions;

/// <summary>
/// Claims-based context for the currently authenticated user.
/// Host-agnostic: usable in ASP.NET Core, workers, Blazor, and unit tests.
/// </summary>
public interface IUserContext
{
    /// <summary>Whether the current principal is authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>The user identifier (<c>sub</c>/<see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/>); null when anonymous.</summary>
    string? UserId { get; }

    /// <summary>The username (<c>name</c>/<see cref="System.Security.Claims.ClaimTypes.Name"/>); null when anonymous or absent.</summary>
    string? Username { get; }

    /// <summary>The email (<c>email</c>/<see cref="System.Security.Claims.ClaimTypes.Email"/>); null when anonymous or absent.</summary>
    string? Email { get; }

    /// <summary>The roles assigned to the current principal.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>All claims carried by the current principal.</summary>
    IReadOnlyList<Claim> Claims { get; }

    /// <summary>
    /// Checks whether the current principal has the given role.
    /// </summary>
    /// <param name="role">The role name (case-insensitive).</param>
    /// <returns>True if the principal is in the role; otherwise false.</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Gets the value of the first claim of the given type.
    /// </summary>
    /// <param name="claimType">The claim type to look up.</param>
    /// <returns>The claim value, or null if not present.</returns>
    string? GetClaim(string claimType);
}
