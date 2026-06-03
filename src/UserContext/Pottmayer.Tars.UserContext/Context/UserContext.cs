using Pottmayer.Tars.UserContext.Abstractions.Context;

namespace Pottmayer.Tars.UserContext.Context;

/// <summary>
/// Immutable implementation of <see cref="IUserContext{TUser}"/>.
/// </summary>
/// <typeparam name="TUser">The user type.</typeparam>
public sealed class UserContext<TUser> : IUserContext<TUser>
    where TUser : class
{
    /// <inheritdoc />
    public bool IsAuthenticated { get; }

    /// <inheritdoc />
    public TUser? User { get; }

    /// <summary>
    /// Creates a new user context.
    /// </summary>
    /// <param name="isAuthenticated">Whether the principal is authenticated.</param>
    /// <param name="userId">The user identifier; null when anonymous.</param>
    /// <param name="user">The resolved user; null when anonymous.</param>
    public UserContext(bool isAuthenticated, TUser? user)
    {
        IsAuthenticated = isAuthenticated;
        User = user;
    }
}
