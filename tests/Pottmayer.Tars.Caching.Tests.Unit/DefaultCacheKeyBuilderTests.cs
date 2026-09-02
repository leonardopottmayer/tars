using FluentAssertions;
using Pottmayer.Tars.Caching;
using Pottmayer.Tars.Caching.Options;

namespace Pottmayer.Tars.Caching.Tests.Unit;

public class DefaultCacheKeyBuilderTests
{
    private static DefaultCacheKeyBuilder Build(CachingOptions options)
        => new(options);

    [Fact]
    public void Build_concatenates_prefix_separator_and_key()
    {
        var builder = Build(new TestCachingOptions { KeyPrefix = "tars", KeySeparator = ":" });

        builder.Build("user:42").Should().Be("tars:user:42");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Build_with_blank_key_throws(string? key)
    {
        var builder = Build(new TestCachingOptions());

        var act = () => builder.Build(key!);

        act.Should().Throw<ArgumentException>();
    }
}
