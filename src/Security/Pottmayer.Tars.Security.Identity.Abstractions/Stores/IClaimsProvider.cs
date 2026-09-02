using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Stores;

/// <summary>
/// Optional contract used by base authenticator classes to build claims for a user.
/// Applications that implement authenticators directly do not need this.
/// </summary>
public interface IClaimsProvider<TUser> where TUser : class
{
    /// <summary>
    /// Builds the claims to include in tokens issued for the given user.
    /// </summary>
    /// <param name="user">The user to build claims for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The claims for the user.</returns>
    Task<IReadOnlyList<ClaimData>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default);
}
