using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Pottmayer.Tars.Caching.Core;
using Pottmayer.Tars.Caching.Core.Options;

namespace Pottmayer.Tars.Caching.Tests.Unit;

public class DefaultCacheKeyBuilderTests
{
    private static DefaultCacheKeyBuilder Build(CacheOptions options)
    {
        var monitor = new Mock<IOptionsMonitor<CacheOptions>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(options);
        return new DefaultCacheKeyBuilder(monitor.Object);
    }

    [Fact]
    public void Build_concatenates_prefix_separator_and_key()
    {
        var builder = Build(new CacheOptions { KeyPrefix = "tars", KeySeparator = ":" });

        builder.Build("user:42").Should().Be("tars:user:42");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Build_with_blank_key_throws(string? key)
    {
        var builder = Build(new CacheOptions());

        var act = () => builder.Build(key!);

        act.Should().Throw<ArgumentException>();
    }
}
