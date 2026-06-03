using Pottmayer.Tars.Security.Identity.Abstractions.Dtos;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Contracts;

/// <summary>
/// Implemented by the host to authenticate via API key and return identity for JWT issuance.
/// Used for endpoint POST /auth/sign-in/api-key (emits JWT + refresh).
/// </summary>
public interface IApiKeyAuthenticator
{
    /// <summary>
    /// Authenticates with the API key and returns identity for token issuance.
    /// </summary>
    /// <param name="request">API key sign-in request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result; null if key is invalid.</returns>
    ValueTask<AuthenticationResult?> AuthenticateAsync(
        ApiKeySignInRequest request,
        CancellationToken cancellationToken = default);
}
