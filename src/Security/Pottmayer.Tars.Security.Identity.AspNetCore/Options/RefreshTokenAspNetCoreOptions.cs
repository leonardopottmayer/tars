namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

/// <summary>
/// ASP.NET Core-specific refresh token settings.
/// </summary>
public sealed class RefreshTokenAspNetCoreOptions
{
    /// <summary>The HTTP header the refresh token is written to in header/hybrid delivery modes; null to disable.</summary>
    public string? RefreshTokenHeaderName { get; init; } = "X-Refresh-Token";
}
