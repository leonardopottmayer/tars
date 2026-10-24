using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Dtos;
using Pottmayer.Tars.Security.Identity.Abstractions.Enums;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Token;

internal sealed class TokenOutputWriter : ITokenOutputWriter
{
    private readonly IOptionsMonitor<IdentityAspNetCoreOptions> _optionsMonitor;
    private readonly IOptionsMonitor<SecurityIdentityOptions> _identityOptionsMonitor;

    public TokenOutputWriter(
        IOptionsMonitor<IdentityAspNetCoreOptions> optionsMonitor,
        IOptionsMonitor<SecurityIdentityOptions> identityOptionsMonitor)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _identityOptionsMonitor = identityOptionsMonitor ?? throw new ArgumentNullException(nameof(identityOptionsMonitor));
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
                AppendCookie(context, accessName, tokenResponse.AccessToken,
                    DateTimeOffset.FromUnixTimeSeconds(tokenResponse.ExpiresAt), cookieOpts.Path, cookieOpts);
                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    AppendCookie(context, refreshName, tokenResponse.RefreshToken,
                        DateTimeOffset.UtcNow.Add(_identityOptionsMonitor.CurrentValue.RefreshToken.Lifetime),
                        cookieOpts.RefreshTokenCookiePath ?? cookieOpts.Path, cookieOpts);
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
        DateTimeOffset expires,
        string path,
        IdentityCookieOptions cookieOpts)
    {
        context.CookiesToAppend.Add(new TokenCookieWriteModel
        {
            Name = name,
            Value = value,
            Path = path,
            HttpOnly = cookieOpts.HttpOnly,
            Secure = cookieOpts.SecurePolicy,
            SameSite = cookieOpts.SameSite,
            ExpiresAt = expires
        });
    }
}
