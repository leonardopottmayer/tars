using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Token;

/// <summary>
/// Issues access tokens from authentication results.
/// </summary>
public interface ITokenIssuer
{
    ValueTask<IssuedTokenResult> IssueAsync(AuthenticationResult result, CancellationToken cancellationToken = default);
}
