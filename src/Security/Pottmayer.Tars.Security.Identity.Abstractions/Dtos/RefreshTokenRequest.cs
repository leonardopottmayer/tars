namespace Pottmayer.Tars.Security.Identity.Abstractions.Dtos;

/// <summary>
/// Request to refresh access token using a refresh token.
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>Refresh token (from cookie, header, or body).</summary>
    public required string RefreshToken { get; init; }
}
