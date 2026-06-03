namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

public sealed class MagicLinkAspNetCoreOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public string TokenQueryParameter { get; init; } = "token";
}
