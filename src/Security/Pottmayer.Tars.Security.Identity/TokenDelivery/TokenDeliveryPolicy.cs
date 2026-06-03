using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Enums;
using Pottmayer.Tars.Security.Identity.Abstractions.TokenDelivery;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Identity.TokenDelivery;

/// <summary>
/// Determines effective token delivery mode from context and options.
/// </summary>
public sealed class TokenDeliveryPolicy
{
    private readonly IOptionsMonitor<IdentityOptions> _optionsMonitor;

    public TokenDeliveryPolicy(IOptionsMonitor<IdentityOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }

    /// <summary>
    /// Resolves the effective delivery mode for reading or writing the token.
    /// </summary>
    public TokenDeliveryMode GetEffectiveMode(TokenDeliveryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var options = _optionsMonitor.CurrentValue.TokenDelivery;
        if (context.ConfiguredMode != TokenDeliveryMode.Hybrid)
            return context.ConfiguredMode;

        if (options.HybridPreferHeader && context.HasAuthorizationHeader)
            return TokenDeliveryMode.HeaderOnly;

        if (options.HybridPreferCookie && context.HasAuthCookie)
            return TokenDeliveryMode.CookieOnly;

        return options.Mode;
    }
}
