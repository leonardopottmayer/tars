namespace Pottmayer.Tars.Security.Identity.Abstractions.Transport;

/// <summary>
/// Transport-agnostic cookie descriptor.
/// The HTTP adapter maps SameSite to Microsoft.AspNetCore.Http.SameSiteMode.
/// </summary>
public sealed class TokenCookieWriteModel
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Path { get; init; }
    public bool HttpOnly { get; init; }
    public bool Secure { get; init; }
    public TokenCookieSameSiteMode SameSite { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

public enum TokenCookieSameSiteMode
{
    None = 0,
    Lax = 1,
    Strict = 2,
    Unspecified = -1
}
