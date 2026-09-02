using FluentAssertions;

namespace Pottmayer.Tars.Caching.Tests.Unit;

public class CachingOptionsTests
{
    [Fact]
    public void Defaults_are_valid()
    {
        new TestCachingOptions().IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData("", ":")]
    [InlineData("tars", "")]
    public void Blank_prefix_or_separator_is_invalid(string prefix, string separator)
    {
        new TestCachingOptions { KeyPrefix = prefix, KeySeparator = separator }.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Non_positive_default_expiration_is_invalid()
    {
        new TestCachingOptions { DefaultAbsoluteExpirationRelativeToNow = TimeSpan.Zero }.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Positive_default_expiration_is_valid()
    {
        new TestCachingOptions { DefaultAbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }.IsValid().Should().BeTrue();
    }
}
