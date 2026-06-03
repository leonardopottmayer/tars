namespace Pottmayer.Tars.Security.Identity.Abstractions.Stores;

/// <summary>
/// Optional store contract used by base authenticator classes.
/// Applications that implement authenticators directly do not need this.
/// </summary>
public interface IUserStore<TUser> where TUser : class
{
    Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<TUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<TUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}
