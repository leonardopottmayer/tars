using FluentAssertions;
using Pottmayer.Tars.Core.Primitives;

namespace Pottmayer.Tars.Core.Tests.Unit;

public class OptionalTests
{
    [Fact]
    public void Absent_is_not_present_and_returns_default()
    {
        var optional = Optional<string>.Absent();

        optional.IsPresent.Should().BeFalse();
        optional.Value.Should().BeNull();
    }

    [Fact]
    public void Some_is_present_and_carries_value()
    {
        var optional = Optional<int>.Some(7);

        optional.IsPresent.Should().BeTrue();
        optional.Value.Should().Be(7);
    }

    [Fact]
    public void Some_with_null_is_present_but_null()
    {
        var optional = Optional<string?>.Some(null);

        optional.IsPresent.Should().BeTrue();
        optional.Value.Should().BeNull();
    }

    [Fact]
    public void Deconstruct_exposes_presence_and_value()
    {
        var (isPresent, value) = Optional<int>.Some(9);

        isPresent.Should().BeTrue();
        value.Should().Be(9);
    }
}
