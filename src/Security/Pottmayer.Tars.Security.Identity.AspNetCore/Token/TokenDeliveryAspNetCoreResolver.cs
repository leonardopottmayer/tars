using Pottmayer.Tars.Security.Identity.Abstractions.Enums;
using Pottmayer.Tars.Security.Identity.Abstractions.TokenDelivery;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;
using Pottmayer.Tars.Security.Identity.Options;
using Pottmayer.Tars.Security.Identity.TokenDelivery;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Token;

internal static class TokenDeliveryAspNetCoreResolver
{
    public static TokenDeliveryMode ResolveEffectiveMode(
        IdentityOptions identityOptions,
        IdentityAspNetCoreOptions aspNetCoreOptions,
        TokenDeliveryPolicy policy,
        bool hasAuthorizationHeader,
        bool hasAuthCookie,
        string? clientTypeHeaderValue)
    {
        if (identityOptions.TokenDelivery.Mode == TokenDeliveryMode.Hybrid
            && !string.IsNullOrWhiteSpace(clientTypeHeaderValue))
        {
            if (string.Equals(clientTypeHeaderValue, aspNetCoreOptions.TokenDelivery.HybridHeaderClientTypeValue, StringComparison.OrdinalIgnoreCase))
                return TokenDeliveryMode.HeaderOnly;

            if (string.Equals(clientTypeHeaderValue, aspNetCoreOptions.TokenDelivery.HybridCookieClientTypeValue, StringComparison.OrdinalIgnoreCase))
                return TokenDeliveryMode.CookieOnly;
        }

        return policy.GetEffectiveMode(new TokenDeliveryContext
        {
            HasAuthorizationHeader = hasAuthorizationHeader,
            HasAuthCookie = hasAuthCookie,
            ClientTypeHeaderValue = clientTypeHeaderValue,
            ConfiguredMode = identityOptions.TokenDelivery.Mode
        });
    }
}
