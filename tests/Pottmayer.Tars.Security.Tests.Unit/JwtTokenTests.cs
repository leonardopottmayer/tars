using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Jwt;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class JwtTokenTests
{
    private const string SigningKey = "this-is-a-very-long-signing-key-for-hmac-sha256-tests-0123456789";

    private static IOptionsMonitor<IdentityOptions> Options(IdentityOptions? options = null)
    {
        options ??= new IdentityOptions
        {
            Jwt = new JwtOptions { SigningKey = SigningKey, Issuer = "tars-test", Audience = "tars-test" },
        };
        var monitor = new Mock<IOptionsMonitor<IdentityOptions>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(options);
        return monitor.Object;
    }

    private static AuthenticationResult Auth() => new()
    {
        Subject = "user-1",
        Claims = [new ClaimData("role", "admin")],
    };

    [Fact]
    public async Task Issued_token_validates_and_exposes_subject_and_claims()
    {
        var options = Options();
        var issuer = new JwtTokenIssuer(options);
        var validator = new JwtTokenValidator(options);

        var issued = await issuer.IssueAsync(Auth());
        var principal = await validator.ValidateAsync(issued.AccessToken);

        issued.AccessToken.Should().NotBeNullOrWhiteSpace();
        issued.Jti.Should().NotBeNullOrWhiteSpace();
        principal.Should().NotBeNull();

        // JwtSecurityTokenHandler remaps inbound "sub" to ClaimTypes.NameIdentifier.
        var subject = principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
        subject.Should().Be("user-1");
        principal.Claims.Should().Contain(c => c.Value == "admin");
    }

    [Fact]
    public async Task Validation_fails_for_token_signed_with_different_key()
    {
        var issued = await new JwtTokenIssuer(Options()).IssueAsync(Auth());

        var otherValidator = new JwtTokenValidator(Options(new IdentityOptions
        {
            Jwt = new JwtOptions { SigningKey = "a-completely-different-signing-key-0123456789-abcdefghij", Issuer = "tars-test", Audience = "tars-test" },
        }));

        (await otherValidator.ValidateAsync(issued.AccessToken)).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    public async Task Validation_returns_null_for_invalid_input(string token)
    {
        (await new JwtTokenValidator(Options()).ValidateAsync(token)).Should().BeNull();
    }

    [Fact]
    public async Task IsValidAsync_reflects_validation_result()
    {
        var options = Options();
        var issued = await new JwtTokenIssuer(options).IssueAsync(Auth());
        var validator = new JwtTokenValidator(options);

        (await validator.IsValidAsync(issued.AccessToken)).Should().BeTrue();
        (await validator.IsValidAsync("garbage")).Should().BeFalse();
    }

    [Fact]
    public async Task Issuing_without_signing_key_throws()
    {
        var issuer = new JwtTokenIssuer(Options(new IdentityOptions { Jwt = new JwtOptions { SigningKey = "" } }));

        var act = async () => await issuer.IssueAsync(Auth());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
