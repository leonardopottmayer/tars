using System.Security.Claims;
using FluentAssertions;
using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;
using Pottmayer.Tars.Multitenancy.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Tests.Unit;

public class ResolversTests
{
    private static TenantResolutionRequest Request(ClaimsPrincipal? principal = null)
        => new() { Principal = principal };

    [Fact]
    public async Task StaticResolver_always_resolves_the_same_key()
    {
        var result = await new StaticTenantResolver("acme").ResolveAsync(Request());

        result.IsResolved.Should().BeTrue();
        result.TenantKey.Should().Be("acme");
        result.TenantCode.Should().Be("acme");
    }

    [Fact]
    public void StaticResolver_rejects_blank_key()
    {
        var act = () => new StaticTenantResolver("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task NullResolver_never_resolves()
    {
        var result = await new NullTenantResolver().ResolveAsync(Request());

        result.IsResolved.Should().BeFalse();
    }

    [Fact]
    public async Task ClaimResolver_resolves_from_claim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_key", "globex")]));

        var result = await new ClaimTenantResolver().ResolveAsync(Request(principal));

        result.IsResolved.Should().BeTrue();
        result.TenantKey.Should().Be("globex");
    }

    [Fact]
    public async Task ClaimResolver_unresolved_without_principal()
    {
        var result = await new ClaimTenantResolver().ResolveAsync(Request(principal: null));

        result.IsResolved.Should().BeFalse();
    }

    [Fact]
    public async Task ClaimResolver_unresolved_when_claim_missing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("other", "x")]));

        var result = await new ClaimTenantResolver("tenant_key").ResolveAsync(Request(principal));

        result.IsResolved.Should().BeFalse();
    }
}
