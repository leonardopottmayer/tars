namespace Pottmayer.Tars.Security.Identity.Abstractions.Token;

/// <summary>
/// Result of issuing an access token.
/// </summary>
/// <param name="AccessToken">The signed access token (JWT).</param>
/// <param name="Jti">The token's unique id (jti claim), used for revocation lookups.</param>
/// <param name="ExpiresAt">Unix timestamp (seconds) when the token expires.</param>
public sealed record IssuedTokenResult(string AccessToken, string Jti, long ExpiresAt);
