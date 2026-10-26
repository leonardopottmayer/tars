using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Jwt;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class JwtTokenTests
{
    private const string SigningKey = "this-is-a-very-long-signing-key-for-hmac-sha256-tests-0123456789";

    private static IOptionsMonitor<SecurityIdentityOptions> Options(SecurityIdentityOptions? options = null)
    {
        options ??= new SecurityIdentityOptions
        {
            Jwt = new JwtOptions { SigningKey = SigningKey, Issuer = "tars-test", Audience = "tars-test" },
        };
        var monitor = new Mock<IOptionsMonitor<SecurityIdentityOptions>>();
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

        var issued = await issuer.IssueAsync(Auth(), TestContext.Current.CancellationToken);
        var principal = await validator.ValidateAsync(issued.AccessToken, TestContext.Current.CancellationToken);

        issued.AccessToken.Should().NotBeNullOrWhiteSpace();
        issued.Jti.Should().NotBeNullOrWhiteSpace();
        principal.Should().NotBeNull();

        // JwtSecurityTokenHandler remaps inbound "sub" to ClaimTypes.NameIdentifier.
        var subject = principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
        subject.Should().Be("user-1");
        principal.Claims.Should().Contain(c => c.Value == "admin");
    }

    [Fact]
    public async Task Validation_fails_for_token_signed_with_another_algorithm_and_the_same_key()
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(SigningKey));
        var token = handler.WriteToken(handler.CreateToken(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Issuer = "tars-test",
            Audience = "tars-test",
            Subject = new ClaimsIdentity([new Claim("sub", "user-1")]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha512),
        }));

        var principal = await new JwtTokenValidator(Options()).ValidateAsync(token, TestContext.Current.CancellationToken);

        principal.Should().BeNull();
    }

    [Fact]
    public async Task Validation_fails_for_token_signed_with_different_key()
    {
        var issued = await new JwtTokenIssuer(Options()).IssueAsync(Auth(), TestContext.Current.CancellationToken);

        var otherValidator = new JwtTokenValidator(Options(new SecurityIdentityOptions
        {
            Jwt = new JwtOptions { SigningKey = "a-completely-different-signing-key-0123456789-abcdefghij", Issuer = "tars-test", Audience = "tars-test" },
        }));

        (await otherValidator.ValidateAsync(issued.AccessToken, TestContext.Current.CancellationToken)).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    public async Task Validation_returns_null_for_invalid_input(string token)
    {
        (await new JwtTokenValidator(Options()).ValidateAsync(token, TestContext.Current.CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task IsValidAsync_reflects_validation_result()
    {
        var options = Options();
        var issued = await new JwtTokenIssuer(options).IssueAsync(Auth(), TestContext.Current.CancellationToken);
        var validator = new JwtTokenValidator(options);

        (await validator.IsValidAsync(issued.AccessToken, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await validator.IsValidAsync("garbage", TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task Issuing_without_signing_key_throws()
    {
        var issuer = new JwtTokenIssuer(Options(new SecurityIdentityOptions { Jwt = new JwtOptions { SigningKey = "" } }));

        var act = async () => await issuer.IssueAsync(Auth());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Missing_revocation_service_is_warned_about_once_per_process()
    {
        var logger = new CapturingLogger();
        JwtTokenValidator.ResetMissingRevocationWarning();

        _ = new JwtTokenValidator(Options(), revocationService: null, logger);
        _ = new JwtTokenValidator(Options(), revocationService: null, logger);

        logger.Warnings.Should().ContainSingle().Which.Should().Contain("ITokenRevocationService");
    }

    private sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger<JwtTokenValidator>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == Microsoft.Extensions.Logging.LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }
}
