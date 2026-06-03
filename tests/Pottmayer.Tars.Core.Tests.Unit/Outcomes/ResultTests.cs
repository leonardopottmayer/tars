using FluentAssertions;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Tests.Unit.Outcomes;

public class ResultTests
{
    [Fact]
    public void Success_creates_a_successful_result_without_errors()
    {
        var result = Result.Success("corr-1");

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Errors.Should().BeEmpty();
        result.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public void Failure_creates_a_failed_result_with_errors()
    {
        var error = Error.Validation("CODE", "message");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void Failure_without_errors_throws()
    {
        var act = () => Result.Failure(Array.Empty<Error>());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Generic_success_carries_value()
    {
        var result = Result<int>.Success(42, "corr");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.CorrelationId.Should().Be("corr");
    }

    [Fact]
    public void Generic_success_with_null_value_throws()
    {
        var act = () => Result<string>.Success(null!);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Generic_failure_has_default_value_and_errors()
    {
        var error = Error.NotFound("X", "not found");

        var result = Result<int>.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(default);
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void Implicit_conversion_from_error_produces_failure()
    {
        Result<int> result = Error.Business("B", "boom");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Type.Should().Be(ErrorType.Business);
    }
}
