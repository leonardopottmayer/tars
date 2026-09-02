using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Token;

/// <summary>
/// Issues access tokens from authentication results.
/// </summary>
public interface ITokenIssuer
{
    /// <summary>
    /// Issues an access token from a successful authentication result.
    /// </summary>
    /// <param name="result">The authentication result to issue a token for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The issued access token.</returns>
    ValueTask<IssuedTokenResult> IssueAsync(AuthenticationResult result, CancellationToken cancellationToken = default);
}
