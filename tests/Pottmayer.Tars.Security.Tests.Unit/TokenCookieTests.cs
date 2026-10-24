using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Dtos;
using Pottmayer.Tars.Security.Identity.Abstractions.Enums;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.DI;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;
using Pottmayer.Tars.Security.Identity.AspNetCore.Token;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class TokenCookieTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddOptions<SecurityIdentityOptions>().Bind(configuration.GetSection(SecurityIdentityOptions.SectionName));
        services.AddOptions<IdentityAspNetCoreOptions>().Bind(configuration.GetSection(IdentityAspNetCoreOptions.SectionName));
        services.AddTarsIdentityTokenOutputWriter();
        return services.BuildServiceProvider();
    }

    private static async Task<TokenCookieWriteModel> WriteRefreshCookie(Dictionary<string, string?> settings)
    {
        using var sp = Build(settings);
        var context = new TokenWriteContext();
        var response = new TokenResponse { AccessToken = "at", RefreshToken = "rt", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds() };

        await sp.GetRequiredService<ITokenOutputWriter>().WriteAsync(context, response, TokenDeliveryMode.CookieOnly);

        return context.CookiesToAppend.Single(c => c.Value == "rt");
    }

    [Fact]
    public async Task Refresh_cookie_expires_with_the_configured_refresh_token_lifetime()
    {
        var cookie = await WriteRefreshCookie(new()
        {
            [$"{SecurityIdentityOptions.SectionName}:RefreshToken:Lifetime"] = "30.00:00:00",
        });

        cookie.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Refresh_cookie_uses_its_own_path_when_configured()
    {
        var cookie = await WriteRefreshCookie(new()
        {
            [$"{IdentityAspNetCoreOptions.SectionName}:Cookie:RefreshTokenCookiePath"] = "/identity",
        });

        cookie.Path.Should().Be("/identity");
    }

    [Fact]
    public async Task Refresh_cookie_falls_back_to_the_shared_path()
    {
        var cookie = await WriteRefreshCookie([]);

        cookie.Path.Should().Be("/");
    }

    [Fact]
    public void Deleting_auth_cookies_targets_the_paths_they_were_written_with()
    {
        var http = new DefaultHttpContext();
        var options = new IdentityAspNetCoreOptions { Cookie = new IdentityCookieOptions { Path = "/app", RefreshTokenCookiePath = "/identity" } };

        HttpContextTokenBridge.DeleteAuthCookies(http, options);

        var setCookies = http.Response.Headers.SetCookie.ToArray();
        setCookies.Should().Contain(c => c!.StartsWith("tars.at=") && c.Contains("path=/app"));
        setCookies.Should().Contain(c => c!.StartsWith("tars.rt=") && c.Contains("path=/identity"));
    }
}
