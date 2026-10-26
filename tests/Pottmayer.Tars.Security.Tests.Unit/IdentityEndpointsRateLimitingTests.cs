using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pottmayer.Tars.Security.Identity.Abstractions.Contracts;
using Pottmayer.Tars.Security.Identity.AspNetCore.Endpoints;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class IdentityEndpointsRateLimitingTests
{
    private static readonly string[] CredentialEndpoints =
    [
        "TarsIdentity_SignInPassword",
        "TarsIdentity_RequestMagicLink",
        "TarsIdentity_ConsumeMagicLink",
        "TarsIdentity_SignInApiKey",
        "TarsIdentity_Refresh",
        "TarsIdentity_OAuthChallenge",
    ];

    [Fact]
    public async Task Configured_policy_is_applied_to_every_anonymous_credential_endpoint()
    {
        var policies = await PoliciesByEndpoint("identity-auth");

        foreach (var name in CredentialEndpoints)
            policies[name].Should().Be("identity-auth", because: $"{name} accepts credentials anonymously");
        policies["TarsIdentity_SignOut"].Should().BeNull();
    }

    [Fact]
    public async Task No_policy_is_applied_when_none_is_configured()
    {
        var policies = await PoliciesByEndpoint(policy: null);

        policies.Values.Should().AllSatisfy(p => p.Should().BeNull());
    }

    private static async Task<Dictionary<string, string?>> PoliciesByEndpoint(string? policy)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{IdentityAspNetCoreOptions.SectionName}:Endpoints:RateLimitingPolicy"] = policy,
        });
        builder.Services.AddOptions<IdentityAspNetCoreOptions>()
            .Bind(builder.Configuration.GetSection(IdentityAspNetCoreOptions.SectionName));
        builder.Services.AddSingleton(Mock.Of<IOAuthUserLinker>());

        await using var app = builder.Build();
        app.MapTarsIdentityEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(d => d.Endpoints)
            .Where(e => e.Metadata.GetMetadata<IEndpointNameMetadata>() is not null)
            .ToDictionary(
                e => e.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName,
                e => e.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
    }
}
