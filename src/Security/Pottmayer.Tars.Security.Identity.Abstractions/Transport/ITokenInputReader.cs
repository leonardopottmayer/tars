namespace Pottmayer.Tars.Security.Identity.Abstractions.Transport;

/// <summary>
/// Reads the access or refresh token from a transport-agnostic context.
/// Implementations live in adapter projects (e.g. Identity.AspNetCore).
/// </summary>
public interface ITokenInputReader
{
    /// <summary>
    /// Reads the access token from the request context, if present.
    /// </summary>
    /// <param name="context">The inbound request context.</param>
    /// <returns>The access token, or null if not present.</returns>
    string? ReadAccessToken(TokenReadContext context);

    /// <summary>
    /// Reads the refresh token from the request context, if present.
    /// </summary>
    /// <param name="context">The inbound request context.</param>
    /// <returns>The refresh token, or null if not present.</returns>
    string? ReadRefreshToken(TokenReadContext context);
}
