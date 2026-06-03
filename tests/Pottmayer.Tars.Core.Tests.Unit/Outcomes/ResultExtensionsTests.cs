using FluentAssertions;
using Pottmayer.Tars.Core.Primitives.Extensions;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Tests.Unit.Outcomes;

public class ResultExtensionsTests
{
    [Fact]
    public void Map_transforms_value_of_success_and_keeps_correlation()
    {
        var result = Result<int>.Success(21, "corr");

        var mapped = result.Map(x => x * 2);

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(42);
        mapped.CorrelationId.Should().Be("corr");
    }

    [Fact]
    public void Map_propagates_errors_without_invoking_mapper()
    {
        var error = Error.NotFound("X", "nope");
        var result = Result<int>.Failure(error);
        var invoked = false;

        var mapped = result.Map(x => { invoked = true; return x.ToString(); });

        invoked.Should().BeFalse();
        mapped.IsFailure.Should().BeTrue();
        mapped.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void WithCorrelationId_sets_id_on_success()
    {
        var result = Result<int>.Success(1).WithCorrelationId("abc");

        result.CorrelationId.Should().Be("abc");
        result.Value.Should().Be(1);
    }

    [Fact]
    public void WithCorrelationId_sets_id_on_failure()
    {
        var result = Result.Failure(Error.Business("B", "x")).WithCorrelationId("abc");

        result.CorrelationId.Should().Be("abc");
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void FirstErrorOrNull_returns_first_error_or_null()
    {
        Result.Failure(Error.Validation("A", "1"), Error.Validation("B", "2"))
            .FirstErrorOrNull()!.Code.Should().Be("A");

        Result.Success().FirstErrorOrNull().Should().BeNull();
    }
}
