namespace Pottmayer.Tars.UserContext.Abstractions.Context;

/// <summary>
/// Creates a user context for the current principal (authenticated or anonymous).
/// </summary>
/// <typeparam name="TUser">The user type.</typeparam>
public interface IUserContextFactory<TUser>
    where TUser : class
{
    /// <summary>
    /// Creates the user context for the current request.
    /// </summary>
    /// <returns>An immutable user context.</returns>
    IUserContext<TUser> Create();
}
