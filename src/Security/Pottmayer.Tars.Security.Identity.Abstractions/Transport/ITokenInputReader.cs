namespace Pottmayer.Tars.Security.Identity.Abstractions.Transport;

/// <summary>
/// Reads the access or refresh token from a transport-agnostic context.
/// Implementations live in adapter projects (e.g. Identity.AspNetCore).
/// </summary>
public interface ITokenInputReader
{
    string? ReadAccessToken(TokenReadContext context);
    string? ReadRefreshToken(TokenReadContext context);
}
