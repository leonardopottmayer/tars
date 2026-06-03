using FluentAssertions;
using Pottmayer.Tars.Caching.Core.Options;

namespace Pottmayer.Tars.Caching.Tests.Unit;

public class CacheOptionsTests
{
    [Fact]
    public void Defaults_are_valid()
    {
        new CacheOptions().IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData("", ":")]
    [InlineData("tars", "")]
    public void Blank_prefix_or_separator_is_invalid(string prefix, string separator)
    {
        new CacheOptions { KeyPrefix = prefix, KeySeparator = separator }.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Non_positive_default_expiration_is_invalid()
    {
        new CacheOptions { DefaultAbsoluteExpirationRelativeToNow = TimeSpan.Zero }.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Positive_default_expiration_is_valid()
    {
        new CacheOptions { DefaultAbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }.IsValid().Should().BeTrue();
    }
}
