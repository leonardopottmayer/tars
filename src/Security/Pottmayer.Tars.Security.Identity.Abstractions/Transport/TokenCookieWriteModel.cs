namespace Pottmayer.Tars.Security.Identity.Abstractions.Transport;

/// <summary>
/// Transport-agnostic cookie descriptor.
/// The HTTP adapter maps SameSite to Microsoft.AspNetCore.Http.SameSiteMode.
/// </summary>
public sealed class TokenCookieWriteModel
{
    /// <summary>The cookie name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The cookie value.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>The cookie path.</summary>
    public string? Path { get; init; }

    /// <summary>Whether the cookie is HttpOnly.</summary>
    public bool HttpOnly { get; init; }

    /// <summary>Whether the cookie requires HTTPS (Secure flag).</summary>
    public bool Secure { get; init; }

    /// <summary>The cookie's SameSite policy.</summary>
    public TokenCookieSameSiteMode SameSite { get; init; }

    /// <summary>When the cookie expires; null for a session cookie.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// Transport-agnostic SameSite cookie policy, mapped by the HTTP adapter to <see cref="Microsoft.AspNetCore.Http.SameSiteMode"/>.
/// </summary>
public enum TokenCookieSameSiteMode
{
    /// <summary>Sent with all requests, including cross-site.</summary>
    None = 0,

    /// <summary>Sent with same-site requests and top-level cross-site navigations.</summary>
    Lax = 1,

    /// <summary>Sent only with same-site requests.</summary>
    Strict = 2,

    /// <summary>No policy specified; the adapter applies its own default.</summary>
    Unspecified = -1
}
