using System.Security.Claims;
using FluentAssertions;
using Pottmayer.Tars.UserContext;

namespace Pottmayer.Tars.UserContext.Tests.Unit;

public class UserContextTests
{
    [Fact]
    public void Anonymous_is_not_authenticated()
    {
        global::Pottmayer.Tars.UserContext.UserContext.Anonymous.IsAuthenticated.Should().BeFalse();
        global::Pottmayer.Tars.UserContext.UserContext.Anonymous.UserId.Should().BeNull();
    }

    [Fact]
    public void Maps_standard_claims_to_properties()
    {
        var ctx = new global::Pottmayer.Tars.UserContext.UserContext(
        [
            new Claim(ClaimTypes.NameIdentifier, "u-1"),
            new Claim(ClaimTypes.Name, "alice"),
            new Claim(ClaimTypes.Email, "alice@example.com"),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "user"),
        ]);

        ctx.IsAuthenticated.Should().BeTrue();
        ctx.UserId.Should().Be("u-1");
        ctx.Username.Should().Be("alice");
        ctx.Email.Should().Be("alice@example.com");
        ctx.Roles.Should().BeEquivalentTo("admin", "user");
    }

    [Fact]
    public void Falls_back_to_jwt_style_claim_names()
    {
        var ctx = new global::Pottmayer.Tars.UserContext.UserContext(
        [
            new Claim("sub", "u-2"),
            new Claim("name", "bob"),
            new Claim("email", "bob@example.com"),
        ]);

        ctx.UserId.Should().Be("u-2");
        ctx.Username.Should().Be("bob");
        ctx.Email.Should().Be("bob@example.com");
    }

    [Fact]
    public void IsInRole_is_case_insensitive()
    {
        var ctx = new global::Pottmayer.Tars.UserContext.UserContext([new Claim(ClaimTypes.Role, "Admin")]);

        ctx.IsInRole("admin").Should().BeTrue();
        ctx.IsInRole("missing").Should().BeFalse();
    }

    [Fact]
    public void GetClaim_returns_value_or_null()
    {
        var ctx = new global::Pottmayer.Tars.UserContext.UserContext([new Claim("custom", "x")]);

        ctx.GetClaim("custom").Should().Be("x");
        ctx.GetClaim("absent").Should().BeNull();
    }
}
