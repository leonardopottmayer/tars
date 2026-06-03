using FluentAssertions;
using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;
using Pottmayer.Tars.Multitenancy.Context;

namespace Pottmayer.Tars.Multitenancy.Tests.Unit;

public class TenantContextTests
{
    [Fact]
    public void Create_defaults_code_to_key()
    {
        var ctx = TenantContext.Create("acme");

        ctx.IsResolved.Should().BeTrue();
        ctx.TenantKey.Should().Be("acme");
        ctx.TenantCode.Should().Be("acme");
    }

    [Fact]
    public void Unresolved_has_no_identity()
    {
        var ctx = TenantContext.Unresolved();

        ctx.IsResolved.Should().BeFalse();
        ctx.TenantKey.Should().BeNull();
    }

    [Fact]
    public void FromResolution_copies_identity()
    {
        var ctx = TenantContext.FromResolution(TenantResolutionResult.Resolved("globex", "GLBX"));

        ctx.IsResolved.Should().BeTrue();
        ctx.TenantKey.Should().Be("globex");
        ctx.TenantCode.Should().Be("GLBX");
    }

    [Fact]
    public void Accessor_stores_and_clears_current()
    {
        var accessor = new TenantContextAccessor();
        accessor.Current.Should().BeNull();

        accessor.SetCurrent(TenantContext.Create("acme"));
        accessor.Current!.TenantKey.Should().Be("acme");

        accessor.SetCurrent(null);
        accessor.Current.Should().BeNull();
    }
}
