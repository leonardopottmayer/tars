using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Options;
using Pottmayer.Tars.Security.Identity.Refresh;
using Pottmayer.Tars.Security.Identity.Stores;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class RefreshTokenServiceTests
{
    private static RefreshTokenService Create(RefreshTokenOptions refresh)
    {
        var monitor = new Mock<IOptionsMonitor<IdentityOptions>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(new IdentityOptions { RefreshToken = refresh });
        return new RefreshTokenService(new InMemoryRefreshTokenStore(), monitor.Object);
    }

    private static IReadOnlyList<ClaimData> Claims() => [new ClaimData("role", "user")];

    [Fact]
    public async Task Issued_token_can_be_consumed_once_with_rotation()
    {
        var service = Create(new RefreshTokenOptions { RotationEnabled = true });

        var issued = await service.IssueAsync("user-1", Claims(), metadata: null);
        var consumed = await service.ConsumeAsync(issued.OpaqueToken);

        issued.OpaqueToken.Should().Contain(":");
        consumed.Should().NotBeNull();
        consumed!.Payload.Subject.Should().Be("user-1");
        consumed.ShouldIssueNewRefreshToken.Should().BeTrue();
    }

    [Fact]
    public async Task With_rotation_a_second_consume_fails()
    {
        var service = Create(new RefreshTokenOptions { RotationEnabled = true, ReuseDetectionEnabled = false });
        var issued = await service.IssueAsync("user-1", Claims(), metadata: null);

        await service.ConsumeAsync(issued.OpaqueToken);
        var second = await service.ConsumeAsync(issued.OpaqueToken);

        second.Should().BeNull();
    }

    [Fact]
    public async Task Without_rotation_token_is_reusable()
    {
        var service = Create(new RefreshTokenOptions { RotationEnabled = false });
        var issued = await service.IssueAsync("user-1", Claims(), metadata: null);

        var first = await service.ConsumeAsync(issued.OpaqueToken);
        var second = await service.ConsumeAsync(issued.OpaqueToken);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.ShouldIssueNewRefreshToken.Should().BeFalse();
    }

    [Fact]
    public async Task Revoked_token_cannot_be_consumed()
    {
        var service = Create(new RefreshTokenOptions { RotationEnabled = true });
        var issued = await service.IssueAsync("user-1", Claims(), metadata: null);

        await service.RevokeAsync(issued.OpaqueToken);
        var consumed = await service.ConsumeAsync(issued.OpaqueToken);

        consumed.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-colon")]
    [InlineData("only:")]
    public async Task Malformed_tokens_return_null(string token)
    {
        var service = Create(new RefreshTokenOptions());

        (await service.ConsumeAsync(token)).Should().BeNull();
    }
}
