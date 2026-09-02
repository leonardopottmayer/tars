namespace Pottmayer.Tars.Security.Identity.Abstractions.Stores;

/// <summary>
/// Optional store contract used by base authenticator classes.
/// Applications that implement authenticators directly do not need this.
/// </summary>
public interface IUserStore<TUser> where TUser : class
{
    /// <summary>Finds a user by id.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null if not found.</returns>
    Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Finds a user by username.</summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null if not found.</returns>
    Task<TUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Finds a user by email.</summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null if not found.</returns>
    Task<TUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}
