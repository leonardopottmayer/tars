using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Pottmayer.Tars.Security.Identity.Abstractions.Stores;
using Pottmayer.Tars.Security.Identity.MagicLink;
using Pottmayer.Tars.Security.Identity.Options;
using Pottmayer.Tars.Security.Identity.Stores;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class MagicLinkTokenServiceTests
{
    private static MagicLinkTokenService Create(IMagicLinkTokenStore store)
    {
        var monitor = new Mock<IOptionsMonitor<SecurityIdentityOptions>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(new SecurityIdentityOptions());
        return new MagicLinkTokenService(store, monitor.Object);
    }

    private static Dictionary<string, object?> Payload() => new() { ["target"] = "user@example.com" };

    [Fact]
    public async Task Store_never_receives_the_raw_token()
    {
        var store = new Mock<IMagicLinkTokenStore>();
        string? storedKey = null;
        store.Setup(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyDictionary<string, object?>, DateTimeOffset, CancellationToken>((key, _, _, _) => storedKey = key)
            .Returns(ValueTask.CompletedTask);

        var issued = await Create(store.Object).IssueAsync(Payload(), TestContext.Current.CancellationToken);

        storedKey.Should().NotBeNullOrEmpty();
        storedKey.Should().NotBe(issued.Token);
        storedKey.Should().NotContain(issued.Token);
    }

    [Fact]
    public async Task Issued_token_can_be_consumed_once()
    {
        var service = Create(new InMemoryMagicLinkTokenStore());
        var issued = await service.IssueAsync(Payload(), TestContext.Current.CancellationToken);

        var first = await service.ConsumeAsync(issued.Token, TestContext.Current.CancellationToken);
        var second = await service.ConsumeAsync(issued.Token, TestContext.Current.CancellationToken);

        first.Should().NotBeNull();
        first!["target"].Should().Be("user@example.com");
        second.Should().BeNull();
    }
}
