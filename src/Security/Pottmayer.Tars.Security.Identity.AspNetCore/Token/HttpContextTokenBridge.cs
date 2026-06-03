using Microsoft.AspNetCore.Http;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Token;

/// <summary>
/// Converts between HttpContext and the transport-agnostic context types used by the Identity core.
/// </summary>
public static class HttpContextTokenBridge
{
    public static TokenReadContext CreateReadContext(HttpContext httpContext)
    {
        return new TokenReadContext
        {
            Headers = httpContext.Request.Headers.ToDictionary(
                x => x.Key,
                x => x.Value.Where(v => v is not null).Select(v => v!).ToArray(),
                StringComparer.OrdinalIgnoreCase),
            Cookies = httpContext.Request.Cookies.ToDictionary(
                x => x.Key,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase)
        };
    }

    public static void ApplyWriteContext(HttpContext httpContext, TokenWriteContext writeContext)
    {
        foreach (var header in writeContext.ResponseHeaders)
            httpContext.Response.Headers[header.Key] = header.Value;

        foreach (var cookie in writeContext.CookiesToAppend)
        {
            httpContext.Response.Cookies.Append(cookie.Name, cookie.Value, new CookieOptions
            {
                HttpOnly = cookie.HttpOnly,
                Secure = cookie.Secure,
                Path = cookie.Path,
                Expires = cookie.ExpiresAt,
                SameSite = MapSameSite(cookie.SameSite)
            });
        }

        foreach (var cookieName in writeContext.CookiesToDelete)
            httpContext.Response.Cookies.Delete(cookieName);
    }

    public static void DeleteAuthCookies(HttpContext httpContext, IdentityAspNetCoreOptions options)
    {
        var cookieOpts = options.Cookie;
        httpContext.Response.Cookies.Delete(cookieOpts.AccessTokenCookieName);
        httpContext.Response.Cookies.Delete(cookieOpts.RefreshTokenCookieName);
    }

    private static Microsoft.AspNetCore.Http.SameSiteMode MapSameSite(TokenCookieSameSiteMode mode)
    {
        return mode switch
        {
            TokenCookieSameSiteMode.None => Microsoft.AspNetCore.Http.SameSiteMode.None,
            TokenCookieSameSiteMode.Lax => Microsoft.AspNetCore.Http.SameSiteMode.Lax,
            TokenCookieSameSiteMode.Strict => Microsoft.AspNetCore.Http.SameSiteMode.Strict,
            _ => Microsoft.AspNetCore.Http.SameSiteMode.Lax
        };
    }
}
