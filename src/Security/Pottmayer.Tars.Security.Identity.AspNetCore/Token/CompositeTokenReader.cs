using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Enums;
using Pottmayer.Tars.Security.Identity.Abstractions.TokenDelivery;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;
using Pottmayer.Tars.Security.Identity.Options;
using Pottmayer.Tars.Security.Identity.TokenDelivery;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Token;

/// <summary>
/// Combines header and cookie readers, using TokenDeliveryPolicy to decide which to try first.
/// </summary>
public sealed class CompositeTokenReader : ITokenInputReader
{
    private readonly ITokenInputReader _headerReader;
    private readonly ITokenInputReader _cookieReader;
    private readonly TokenDeliveryPolicy _policy;
    private readonly IOptionsMonitor<IdentityOptions> _identityOptionsMonitor;
    private readonly IOptionsMonitor<IdentityAspNetCoreOptions> _identityAspNetCoreOptionsMonitor;

    public CompositeTokenReader(
        HeaderTokenReader headerReader,
        CookieTokenReader cookieReader,
        TokenDeliveryPolicy policy,
        IOptionsMonitor<IdentityOptions> identityOptionsMonitor,
        IOptionsMonitor<IdentityAspNetCoreOptions> identityAspNetCoreOptionsMonitor)
    {
        _headerReader = headerReader ?? throw new ArgumentNullException(nameof(headerReader));
        _cookieReader = cookieReader ?? throw new ArgumentNullException(nameof(cookieReader));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _identityOptionsMonitor = identityOptionsMonitor ?? throw new ArgumentNullException(nameof(identityOptionsMonitor));
        _identityAspNetCoreOptionsMonitor = identityAspNetCoreOptionsMonitor ?? throw new ArgumentNullException(nameof(identityAspNetCoreOptionsMonitor));
    }

    public string? ReadAccessToken(TokenReadContext context)
    {
        var mode = BuildDeliveryContext(context).ConfiguredMode;

        return mode switch
        {
            TokenDeliveryMode.HeaderOnly => _headerReader.ReadAccessToken(context),
            TokenDeliveryMode.CookieOnly => _cookieReader.ReadAccessToken(context),
            TokenDeliveryMode.Hybrid => _headerReader.ReadAccessToken(context) ?? _cookieReader.ReadAccessToken(context),
            _ => _headerReader.ReadAccessToken(context) ?? _cookieReader.ReadAccessToken(context)
        };
    }

    public string? ReadRefreshToken(TokenReadContext context)
    {
        var fromCookie = _cookieReader.ReadRefreshToken(context);
        if (!string.IsNullOrEmpty(fromCookie))
            return fromCookie;
        return _headerReader.ReadRefreshToken(context);
    }

    private TokenDeliveryContext BuildDeliveryContext(TokenReadContext context)
    {
        var options = _identityOptionsMonitor.CurrentValue;
        var aspOptions = _identityAspNetCoreOptionsMonitor.CurrentValue;
        var hasAuthHeader = context.Headers.TryGetValue("Authorization", out var authValues)
            && !string.IsNullOrEmpty(authValues.FirstOrDefault());
        var cookieName = aspOptions.Cookie.AccessTokenCookieName;
        var hasCookie = context.Cookies.ContainsKey(cookieName);
        var clientTypeHeader = aspOptions.TokenDelivery.HybridClientTypeHeader;
        context.Headers.TryGetValue(clientTypeHeader, out var clientTypeValues);
        var clientType = clientTypeValues?.FirstOrDefault();
        var effectiveMode = TokenDeliveryAspNetCoreResolver.ResolveEffectiveMode(
            options,
            aspOptions,
            _policy,
            hasAuthHeader,
            hasCookie,
            clientType);

        return new TokenDeliveryContext
        {
            HasAuthorizationHeader = hasAuthHeader,
            HasAuthCookie = hasCookie,
            ClientTypeHeaderValue = clientType,
            ConfiguredMode = effectiveMode
        };
    }
}
