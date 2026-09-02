namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

/// <summary>
/// ASP.NET Core-specific magic link settings.
/// </summary>
public sealed class MagicLinkAspNetCoreOptions
{
    /// <summary>The base URL the magic link points to (the frontend page that consumes the token).</summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>The query string parameter name the magic link token is appended as.</summary>
    public string TokenQueryParameter { get; init; } = "token";
}
