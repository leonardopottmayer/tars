using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Contracts;

/// <summary>
/// Implemented by the host to resolve the user identity when a magic link token is consumed.
/// The framework validates the token and calls this to get the final identity.
/// </summary>
public interface IMagicLinkIdentityResolver
{
    /// <summary>
    /// Resolves the user identity from the consumed magic link (e.g. from the target identifier stored with the token).
    /// </summary>
    /// <param name="magicLinkTokenPayload">Payload that was stored with the magic link (e.g. target, metadata).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result; null if identity cannot be resolved.</returns>
    ValueTask<AuthenticationResult?> ResolveAsync(
        IReadOnlyDictionary<string, object?> magicLinkTokenPayload,
        CancellationToken cancellationToken = default);
}
