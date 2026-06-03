namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

public sealed class RefreshTokenAspNetCoreOptions
{
    public string? RefreshTokenHeaderName { get; init; } = "X-Refresh-Token";
}
