using Pottmayer.Tars.Security.Identity.Abstractions.Enums;

namespace Pottmayer.Tars.Security.Identity.Options;

/// <summary>
/// Token delivery mode and hybrid rules.
/// </summary>
public sealed class TokenDeliveryOptions
{
    /// <summary>Default delivery mode.</summary>
    public TokenDeliveryMode Mode { get; init; } = TokenDeliveryMode.HeaderOnly;

    /// <summary>When Mode is Hybrid: prefer header if present.</summary>
    public bool HybridPreferHeader { get; init; } = true;

    /// <summary>When Mode is Hybrid: prefer cookie if present (and no header).</summary>
    public bool HybridPreferCookie { get; init; } = true;
}
