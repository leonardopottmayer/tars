using Pottmayer.Tars.Security.Identity.Abstractions.Dtos;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Contracts;

/// <summary>
/// Implemented by the host to validate username/password and return identity.
/// The framework orchestrates the flow; this contract performs the actual authentication.
/// </summary>
public interface IPasswordAuthenticator
{
    /// <summary>
    /// Authenticates the user with the given credentials.
    /// </summary>
    /// <param name="request">Password sign-in request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result if successful; null if credentials are invalid.</returns>
    ValueTask<AuthenticationResult?> AuthenticateAsync(
        PasswordSignInRequest request,
        CancellationToken cancellationToken = default);
}
