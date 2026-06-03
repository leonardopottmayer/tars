namespace Pottmayer.Tars.Security.Identity.Abstractions.Dtos;

/// <summary>
/// Request to sign in with an API key and receive JWT + refresh token.
/// </summary>
public sealed record ApiKeySignInRequest
{
    /// <summary>API key value (e.g. from header or body).</summary>
    public required string ApiKey { get; init; }
}
