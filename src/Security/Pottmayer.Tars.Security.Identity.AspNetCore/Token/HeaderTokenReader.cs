using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Token;

public sealed class HeaderTokenReader : ITokenInputReader
{
    private const string BearerPrefix = "Bearer ";
    private readonly IOptionsMonitor<IdentityAspNetCoreOptions> _optionsMonitor;

    public HeaderTokenReader(IOptionsMonitor<IdentityAspNetCoreOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }

    public string? ReadAccessToken(TokenReadContext context)
    {
        if (!context.Headers.TryGetValue("Authorization", out var values))
            return null;
        var auth = values.FirstOrDefault();
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return null;
        return auth[BearerPrefix.Length..].Trim();
    }

    public string? ReadRefreshToken(TokenReadContext context)
    {
        var headerName = _optionsMonitor.CurrentValue.RefreshToken.RefreshTokenHeaderName ?? "X-Refresh-Token";
        if (!context.Headers.TryGetValue(headerName, out var values))
            return null;
        return values.FirstOrDefault();
    }
}
