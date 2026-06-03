namespace Pottmayer.Tars.Security.Identity.Abstractions.Dtos;

/// <summary>
/// Response containing issued tokens (when delivery is BodyOnly or for API clients).
/// </summary>
public sealed record TokenResponse
{
    /// <summary>JWT access token.</summary>
    public required string AccessToken { get; init; }

    /// <summary>Refresh token (if refresh is enabled).</summary>
    public string? RefreshToken { get; init; }

    /// <summary>Access token expiration (Unix seconds).</summary>
    public required long ExpiresAt { get; init; }

    /// <summary>Token type (e.g. "Bearer").</summary>
    public string TokenType { get; init; } = "Bearer";
}
