using System.Security.Claims;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Token;

/// <summary>
/// Validates access tokens (signature, claims, expiration, revocation).
/// </summary>
public interface ITokenValidator
{
    ValueTask<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken cancellationToken = default);
    ValueTask<bool> IsValidAsync(string token, CancellationToken cancellationToken = default);
}
