namespace Pottmayer.Tars.UserContext.Abstractions.Context;

/// <summary>
/// Typed context for the currently authenticated user.
/// </summary>
/// <typeparam name="TUser">The user type (e.g. DTO or domain user).</typeparam>
public interface IUserContext<out TUser>
    where TUser : class
{
    /// <summary>
    /// Whether the current principal is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The resolved user instance; null when anonymous.
    /// </summary>
    TUser? User { get; }
}
