using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Contracts;

/// <summary>
/// Implemented by the host to validate an API key and return a principal (claims).
/// Used when API Key is the authentication scheme (no JWT issued).
/// </summary>
public interface IApiKeyValidator
{
    /// <summary>
    /// Validates the API key and returns the identity (subject + claims) if valid.
    /// </summary>
    /// <param name="apiKey">Raw API key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result; null if key is invalid.</returns>
    ValueTask<AuthenticationResult?> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default);
}
