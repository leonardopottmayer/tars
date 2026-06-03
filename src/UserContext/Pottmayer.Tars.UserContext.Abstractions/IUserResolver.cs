using System.Security.Claims;

namespace Pottmayer.Tars.UserContext.Abstractions;

/// <summary>
/// Resolves a typed user instance from a claims principal.
/// </summary>
/// <typeparam name="TUser">The user type.</typeparam>
public interface IUserResolver<TUser>
    where TUser : class
{
    /// <summary>
    /// Resolves the user from the given principal.
    /// </summary>
    /// <param name="principal">The authenticated claims principal.</param>
    /// <returns>The resolved user instance.</returns>
    TUser Resolve(ClaimsPrincipal principal);
}
