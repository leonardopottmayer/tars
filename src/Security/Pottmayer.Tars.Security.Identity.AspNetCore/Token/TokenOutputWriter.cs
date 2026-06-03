using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Dtos;
using Pottmayer.Tars.Security.Identity.Abstractions.Enums;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Token;

internal sealed class TokenOutputWriter : ITokenOutputWriter
{
    private readonly IOptionsMonitor<IdentityAspNetCoreOptions> _optionsMonitor;

    public TokenOutputWriter(IOptionsMonitor<IdentityAspNetCoreOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }

    public Task WriteAsync(
        TokenWriteContext context,
        TokenResponse tokenResponse,
        TokenDeliveryMode effectiveMode,
        CancellationToken cancellationToken = default)
    {
        var aspNetCoreOptions = _optionsMonitor.CurrentValue;
        var cookieOpts = aspNetCoreOptions.Cookie;
        var accessName = cookieOpts.AccessTokenCookieName;
        var refreshName = cookieOpts.RefreshTokenCookieName;

        switch (effectiveMode)
        {
            case TokenDeliveryMode.CookieOnly:
                AppendCookie(context, accessName, tokenResponse.AccessToken, tokenResponse.ExpiresAt, cookieOpts);
                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    AppendCookie(context, refreshName, tokenResponse.RefreshToken, null, cookieOpts, longLived: true);
                break;

            case TokenDeliveryMode.HeaderOnly:
            case TokenDeliveryMode.Hybrid:
                context.ResponseHeaders["Cache-Control"] = "no-store";
                context.ResponseHeaders["Authorization"] = $"{tokenResponse.TokenType} {tokenResponse.AccessToken}";
                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    context.ResponseHeaders[aspNetCoreOptions.RefreshToken.RefreshTokenHeaderName ?? "X-Refresh-Token"] = tokenResponse.RefreshToken;
                break;

            case TokenDeliveryMode.BodyOnly:
                context.ResponseHeaders["Cache-Control"] = "no-store";
                context.Body = new
                {
                    access_token = tokenResponse.AccessToken,
                    refresh_token = tokenResponse.RefreshToken,
                    expires_at = tokenResponse.ExpiresAt,
                    token_type = tokenResponse.TokenType
                };
                break;

            default:
                context.ResponseHeaders["Authorization"] = $"{tokenResponse.TokenType} {tokenResponse.AccessToken}";
                break;
        }

        return Task.CompletedTask;
    }

    private static void AppendCookie(
        TokenWriteContext context,
        string name,
        string value,
        long? expiresAt,
        IdentityCookieOptions cookieOpts,
        bool longLived = false)
    {
        DateTimeOffset? expires = null;
        if (expiresAt.HasValue)
            expires = DateTimeOffset.FromUnixTimeSeconds(expiresAt.Value);
        else if (longLived)
            expires = DateTimeOffset.UtcNow.AddDays(7);

        context.CookiesToAppend.Add(new TokenCookieWriteModel
        {
            Name = name,
            Value = value,
            Path = cookieOpts.Path,
            HttpOnly = cookieOpts.HttpOnly,
            Secure = cookieOpts.SecurePolicy,
            SameSite = (TokenCookieSameSiteMode)cookieOpts.SameSite,
            ExpiresAt = expires
        });
    }
}
