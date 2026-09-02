using System.Security.Claims;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Token;

/// <summary>
/// Validates access tokens (signature, claims, expiration, revocation).
/// </summary>
public interface ITokenValidator
{
    /// <summary>
    /// Validates a token and returns the resulting principal.
    /// </summary>
    /// <param name="token">The access token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validated principal, or null if the token is invalid, expired, or revoked.</returns>
    ValueTask<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a token is valid without materializing its principal.
    /// </summary>
    /// <param name="token">The access token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if valid; otherwise false.</returns>
    ValueTask<bool> IsValidAsync(string token, CancellationToken cancellationToken = default);
}
