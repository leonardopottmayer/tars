using AwesomeAssertions;
using Moq;
using Pottmayer.Tars.Security.Identity.Abstractions.Dtos;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Abstractions.Stores;
using Pottmayer.Tars.Security.Identity.Auth;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class PasswordAuthenticatorBaseTests
{
    public sealed record TestUser(string Id);

    private sealed class TestAuthenticator(IUserStore<TestUser> users, IClaimsProvider<TestUser> claims)
        : PasswordAuthenticatorBase<TestUser>(users, claims)
    {
        public List<string> DummyVerifications { get; } = [];

        protected override string GetSubject(TestUser user) => user.Id;

        protected override ValueTask<bool> VerifyPasswordAsync(TestUser user, string password, CancellationToken cancellationToken)
            => ValueTask.FromResult(password == "right");

        protected override ValueTask VerifyPasswordForMissingUserAsync(string password, CancellationToken cancellationToken)
        {
            DummyVerifications.Add(password);
            return ValueTask.CompletedTask;
        }
    }

    private static TestAuthenticator Create(TestUser? user)
    {
        var users = new Mock<IUserStore<TestUser>>();
        users.Setup(u => u.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var claims = new Mock<IClaimsProvider<TestUser>>();
        claims.Setup(c => c.GetClaimsAsync(It.IsAny<TestUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ClaimData>());
        return new TestAuthenticator(users.Object, claims.Object);
    }

    private static PasswordSignInRequest Request(string password) => new() { Username = "ana", Password = password };

    [Fact]
    public async Task Unknown_user_runs_the_dummy_verification_and_fails()
    {
        var authenticator = Create(user: null);

        var result = await authenticator.AuthenticateAsync(Request("guess"), TestContext.Current.CancellationToken);

        result.Should().BeNull();
        authenticator.DummyVerifications.Should().Equal("guess");
    }

    [Fact]
    public async Task Known_user_does_not_run_the_dummy_verification()
    {
        var authenticator = Create(new TestUser("user-1"));

        var result = await authenticator.AuthenticateAsync(Request("right"), TestContext.Current.CancellationToken);

        result!.Subject.Should().Be("user-1");
        authenticator.DummyVerifications.Should().BeEmpty();
    }
}
