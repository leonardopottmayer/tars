namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

/// <summary>
/// ASP.NET Core-specific token delivery settings for hybrid mode.
/// </summary>
public sealed class TokenDeliveryAspNetCoreOptions
{
    /// <summary>The request header used to signal the caller's client type in hybrid delivery.</summary>
    public string HybridClientTypeHeader { get; init; } = "X-Client-Type";

    /// <summary>The <see cref="HybridClientTypeHeader"/> value that selects cookie delivery.</summary>
    public string HybridCookieClientTypeValue { get; init; } = "web";

    /// <summary>The <see cref="HybridClientTypeHeader"/> value that selects header delivery.</summary>
    public string HybridHeaderClientTypeValue { get; init; } = "api";
}
