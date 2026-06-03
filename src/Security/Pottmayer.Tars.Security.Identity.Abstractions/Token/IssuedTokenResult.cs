namespace Pottmayer.Tars.Security.Identity.Abstractions.Token;

/// <summary>
/// Result of issuing an access token.
/// </summary>
public sealed record IssuedTokenResult(string AccessToken, string Jti, long ExpiresAt);
