using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Token;

public sealed class CookieTokenReader : ITokenInputReader
{
    private readonly IOptionsMonitor<IdentityAspNetCoreOptions> _optionsMonitor;

    public CookieTokenReader(IOptionsMonitor<IdentityAspNetCoreOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }

    public string? ReadAccessToken(TokenReadContext context)
    {
        var name = _optionsMonitor.CurrentValue.Cookie.AccessTokenCookieName;
        context.Cookies.TryGetValue(name, out var value);
        return value;
    }

    public string? ReadRefreshToken(TokenReadContext context)
    {
        var name = _optionsMonitor.CurrentValue.Cookie.RefreshTokenCookieName;
        context.Cookies.TryGetValue(name, out var value);
        return value;
    }
}
