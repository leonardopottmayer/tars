using AwesomeAssertions;
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
        var monitor = new Mock<IOptionsMonitor<SecurityIdentityOptions>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(new SecurityIdentityOptions { RefreshToken = refresh });
        return new RefreshTokenService(new InMemoryRefreshTokenStore(), monitor.Object);
    }

    private static IReadOnlyList<ClaimData> Claims() => [new ClaimData("role", "user")];

    [Fact]
    public async Task Issued_token_can_be_consumed_once_with_rotation()
    {
        var service = Create(new RefreshTokenOptions { RotationEnabled = true });

        var issued = await service.IssueAsync("user-1", Claims(), metadata: null, cancellationToken: TestContext.Current.CancellationToken);
        var consumed = await service.ConsumeAsync(issued.OpaqueToken, TestContext.Current.CancellationToken);

        issued.OpaqueToken.Should().Contain(":");
        consumed.Should().NotBeNull();
        consumed!.Payload.Subject.Should().Be("user-1");
        consumed.ShouldIssueNewRefreshToken.Should().BeTrue();
    }

    [Fact]
    public async Task With_rotation_a_second_consume_fails()
    {
        var service = Create(new RefreshTokenOptions { RotationEnabled = true, ReuseDetectionEnabled = false });
        var issued = await service.IssueAsync("user-1", Claims(), metadata: null, cancellationToken: TestContext.Current.CancellationToken);

        await service.ConsumeAsync(issued.OpaqueToken, TestContext.Current.CancellationToken);
        var second = await service.ConsumeAsync(issued.OpaqueToken, TestContext.Current.CancellationToken);

        second.Should().BeNull();
    }

    [Fact]
    public async Task Without_rotation_token_is_reusable()
    {
        var service = Create(new RefreshTokenOptions { RotationEnabled = false });
        var issued = await service.IssueAsync("user-1", Claims(), metadata: null, cancellationToken: TestContext.Current.CancellationToken);

        var first = await service.ConsumeAsync(issued.OpaqueToken, TestContext.Current.CancellationToken);
        var second = await service.ConsumeAsync(issued.OpaqueToken, TestContext.Current.CancellationToken);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.ShouldIssueNewRefreshToken.Should().BeFalse();
    }

    [Fact]
    public async Task Revoked_token_cannot_be_consumed()
    {
        var service = Create(new RefreshTokenOptions { RotationEnabled = true });
        var issued = await service.IssueAsync("user-1", Claims(), metadata: null, cancellationToken: TestContext.Current.CancellationToken);

        await service.RevokeAsync(issued.OpaqueToken, TestContext.Current.CancellationToken);
        var consumed = await service.ConsumeAsync(issued.OpaqueToken, TestContext.Current.CancellationToken);

        consumed.Should().BeNull();
    }

    [Fact]
    public async Task With_rotation_concurrent_consumes_of_the_same_token_succeed_only_once()
    {
        var service = Create(new RefreshTokenOptions { RotationEnabled = true, ReuseDetectionEnabled = false });

        for (var round = 0; round < 200; round++)
        {
            var issued = await service.IssueAsync("user-1", Claims(), metadata: null, cancellationToken: TestContext.Current.CancellationToken);
            using var start = new ManualResetEventSlim();
            var consumers = Enumerable.Range(0, 8)
                .Select(_ => Task.Run(async () =>
                {
                    start.Wait();
                    return await service.ConsumeAsync(issued.OpaqueToken);
                }))
                .ToArray();

            start.Set();
            var results = await Task.WhenAll(consumers);

            results.Count(r => r is not null).Should().Be(1);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-colon")]
    [InlineData("only:")]
    public async Task Malformed_tokens_return_null(string token)
    {
        var service = Create(new RefreshTokenOptions());

        (await service.ConsumeAsync(token, TestContext.Current.CancellationToken)).Should().BeNull();
    }
}
