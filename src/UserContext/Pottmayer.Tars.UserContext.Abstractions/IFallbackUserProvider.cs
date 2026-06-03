namespace Pottmayer.Tars.UserContext.Abstractions;

/// <summary>
/// Provides a fallback user when the current principal has no claims or is not authenticated.
/// Implement this interface and register it in DI to enable a default user (e.g. for local/dev or system operations).
/// Implementations may perform async work (e.g. database access).
/// </summary>
/// <typeparam name="TUser">The user type.</typeparam>
public interface IFallbackUserProvider<TUser>
    where TUser : class
{
    /// <summary>
    /// Returns the fallback user to use when there is no authenticated principal or required claims are missing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The fallback user instance, or null if no fallback should be used.</returns>
    Task<TUser?> GetFallbackUserAsync(CancellationToken cancellationToken = default);
}
