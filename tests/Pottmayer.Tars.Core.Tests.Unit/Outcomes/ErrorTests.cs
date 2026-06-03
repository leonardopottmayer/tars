using FluentAssertions;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Tests.Unit.Outcomes;

public class ErrorTests
{
    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Business)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    [InlineData(ErrorType.Unexpected)]
    public void Factory_methods_set_the_expected_type(ErrorType type)
    {
        var error = type switch
        {
            ErrorType.Validation => Error.Validation("C", "m"),
            ErrorType.Business => Error.Business("C", "m"),
            ErrorType.NotFound => Error.NotFound("C", "m"),
            ErrorType.Conflict => Error.Conflict("C", "m"),
            ErrorType.Unauthorized => Error.Unauthorized("C", "m"),
            ErrorType.Forbidden => Error.Forbidden("C", "m"),
            _ => Error.Unexpected("C", "m"),
        };

        error.Type.Should().Be(type);
        error.Code.Should().Be("C");
        error.Message.Should().Be("m");
    }

    [Fact]
    public void Default_type_is_unexpected()
    {
        var error = new Error("C", "m");

        error.Type.Should().Be(ErrorType.Unexpected);
    }

    [Fact]
    public void Carries_optional_metadata()
    {
        var metadata = new Dictionary<string, object?> { ["field"] = "email" };

        var error = Error.Validation("C", "m", metadata);

        error.Metadata.Should().NotBeNull();
        error.Metadata!["field"].Should().Be("email");
    }

    [Fact]
    public void Records_with_same_values_are_equal()
    {
        var a = Error.NotFound("C", "m");
        var b = Error.NotFound("C", "m");

        a.Should().Be(b);
    }
}
