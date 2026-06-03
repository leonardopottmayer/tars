namespace Pottmayer.Tars.UserContext.Abstractions.Context;

/// <summary>
/// Provides access to the current request's user context.
/// </summary>
/// <typeparam name="TUser">The user type.</typeparam>
public interface IUserContextAccessor<TUser>
    where TUser : class
{
    /// <summary>
    /// The current user context (authenticated or anonymous).
    /// </summary>
    IUserContext<TUser> Context { get; }
}
