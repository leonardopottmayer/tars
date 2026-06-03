namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

public sealed class IdentityCookieOptions
{
    public string AccessTokenCookieName { get; init; } = "tars.at";
    public string RefreshTokenCookieName { get; init; } = "tars.rt";
    public string Path { get; init; } = "/";
    public SameSiteMode SameSite { get; init; } = SameSiteMode.Lax;
    public bool HttpOnly { get; init; } = true;
    public bool SecurePolicy { get; init; } = true;
}

public enum SameSiteMode
{
    None = 0,
    Lax = 1,
    Strict = 2,
    Unspecified = -1
}
