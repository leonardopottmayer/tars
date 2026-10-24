using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Security.Identity.AspNetCore.Endpoints;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class OAuthChallengeEndpointTests
{
    [Fact]
    public async Task Challenge_redirects_to_the_configured_callback_path()
    {
        var properties = await Challenge(new()
        {
            [$"{IdentityAspNetCoreOptions.SectionName}:Endpoints:OAuthCallbackPath"] = "/auth/callback",
        });

        properties!.RedirectUri.Should().Be("/auth/callback");
    }

    [Fact]
    public async Task Challenge_uses_the_default_callback_path_when_none_is_configured()
    {
        var properties = await Challenge([]);

        properties!.RedirectUri.Should().Be(IdentityEndpointsOptions.DefaultOAuthCallbackPath);
    }

    private static async Task<AuthenticationProperties?> Challenge(Dictionary<string, string?> settings)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddOptions<IdentityAspNetCoreOptions>()
            .Bind(builder.Configuration.GetSection(IdentityAspNetCoreOptions.SectionName));
        var auth = new CapturingAuthenticationService();
        builder.Services.AddSingleton<IAuthenticationService>(auth);

        await using var app = builder.Build();
        app.MapTarsIdentityOAuthChallengeEndpoint();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(d => d.Endpoints)
            .Single(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "TarsIdentity_OAuthChallenge");

        using var scope = app.Services.CreateScope();
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        http.Request.RouteValues["provider"] = "google";
        await endpoint.RequestDelegate!(http);

        return auth.ChallengeProperties;
    }

    private sealed class CapturingAuthenticationService : IAuthenticationService
    {
        public AuthenticationProperties? ChallengeProperties { get; private set; }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            ChallengeProperties = properties;
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }
}
