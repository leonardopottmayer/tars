using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Stores;

/// <summary>
/// Optional contract used by base authenticator classes to build claims for a user.
/// Applications that implement authenticators directly do not need this.
/// </summary>
public interface IClaimsProvider<TUser> where TUser : class
{
    Task<IReadOnlyList<ClaimData>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default);
}
