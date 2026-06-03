namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

public sealed class TokenDeliveryAspNetCoreOptions
{
    public string HybridClientTypeHeader { get; init; } = "X-Client-Type";
    public string HybridCookieClientTypeValue { get; init; } = "web";
    public string HybridHeaderClientTypeValue { get; init; } = "api";
}
