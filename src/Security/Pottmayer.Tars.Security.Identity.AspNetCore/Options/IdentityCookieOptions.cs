using Pottmayer.Tars.Security.Identity.Abstractions.Transport;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

/// <summary>
/// Settings for the access/refresh token cookies.
/// </summary>
public sealed class IdentityCookieOptions
{
    /// <summary>The access token cookie name.</summary>
    public string AccessTokenCookieName { get; init; } = "tars.at";

    /// <summary>The refresh token cookie name.</summary>
    public string RefreshTokenCookieName { get; init; } = "tars.rt";

    /// <summary>The cookie path.</summary>
    public string Path { get; init; } = "/";

    /// <summary>
    /// Path for the refresh token cookie; <c>null</c> uses <see cref="Path"/>. Set it to the identity base path
    /// (e.g. <c>/identity</c>) so the browser sends the refresh token only to the refresh and sign-out endpoints.
    /// </summary>
    public string? RefreshTokenCookiePath { get; init; }

    /// <summary>The cookie's SameSite policy.</summary>
    public TokenCookieSameSiteMode SameSite { get; init; } = TokenCookieSameSiteMode.Lax;

    /// <summary>Whether the cookies are HttpOnly. Default: true.</summary>
    public bool HttpOnly { get; init; } = true;

    /// <summary>Whether the cookies require HTTPS (Secure flag). Default: true.</summary>
    public bool SecurePolicy { get; init; } = true;
}
