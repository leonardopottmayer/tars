using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Contracts;

/// <summary>
/// Implemented by the host to link external OAuth identity to local identity.
/// Decides whether to create user, link account, or reject.
/// </summary>
public interface IOAuthUserLinker
{
    /// <summary>
    /// Processes the external OAuth identity and returns the local identity (subject + claims).
    /// </summary>
    /// <param name="provider">OAuth provider name (e.g. "Google", "GitHub").</param>
    /// <param name="externalClaims">Claims from the external provider.</param>
    /// <param name="externalId">External provider user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result for the local user; null to reject (e.g. not allowed).</returns>
    ValueTask<AuthenticationResult?> LinkAsync(
        string provider,
        IReadOnlyDictionary<string, string?> externalClaims,
        string externalId,
        CancellationToken cancellationToken = default);
}
