using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Contracts;

/// <summary>
/// Implemented by the host to authorize refresh and optionally enrich claims.
/// Called when a refresh token is used; the framework validates the token and delegates here.
/// Return null to reject refresh (e.g. user disabled).
/// </summary>
public interface IRefreshAuthorizationHandler
{
    /// <summary>
    /// Authorizes the refresh and returns the identity to use for the new tokens.
    /// </summary>
    /// <param name="subject">Subject from the refresh token.</param>
    /// <param name="claims">Claims from the refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result for new tokens; null to reject refresh.</returns>
    ValueTask<AuthenticationResult?> AuthorizeAsync(
        string subject,
        IReadOnlyList<ClaimData> claims,
        CancellationToken cancellationToken = default);
}
