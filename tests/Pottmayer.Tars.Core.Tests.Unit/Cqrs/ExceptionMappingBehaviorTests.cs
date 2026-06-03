using FluentAssertions;
using Pottmayer.Tars.Core.Cqrs.Behaviors;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Tests.Unit.Cqrs;

public class ExceptionMappingBehaviorTests
{
    private sealed record Cmd : IRequest<Result<int>>;
    private sealed record VoidCmd : IRequest<Result>;

    private sealed class ExpectedFailure(params Error[] errors) : Exception, IExpectedException
    {
        public IReadOnlyList<Error> Errors { get; } = errors;
    }

    [Fact]
    public async Task Passes_through_successful_result()
    {
        var behavior = new ExceptionMappingBehavior<Cmd, Result<int>>();

        var result = await behavior.Handle(new Cmd(), () => ValueTask.FromResult(Result<int>.Success(5)), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task Maps_expected_exception_to_failure_result()
    {
        var error = Error.Validation("FIELD", "invalid");
        var behavior = new ExceptionMappingBehavior<Cmd, Result<int>>();

        var result = await behavior.Handle(
            new Cmd(),
            () => throw new ExpectedFailure(error),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public async Task Maps_expected_exception_for_non_generic_result()
    {
        var error = Error.Business("B", "x");
        var behavior = new ExceptionMappingBehavior<VoidCmd, Result>();

        var result = await behavior.Handle(
            new VoidCmd(),
            () => throw new ExpectedFailure(error),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public async Task Rethrows_unexpected_exception()
    {
        var behavior = new ExceptionMappingBehavior<Cmd, Result<int>>();

        var act = async () => await behavior.Handle(
            new Cmd(),
            () => throw new InvalidOperationException("boom"),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public async Task Uses_custom_mapper_when_configured()
    {
        var config = new ExceptionMappingConfiguration
        {
            CustomMapper = ex => [Error.Conflict("MAPPED", ex.Message)],
        };
        var behavior = new ExceptionMappingBehavior<Cmd, Result<int>>(config);

        var result = await behavior.Handle(
            new Cmd(),
            () => throw new InvalidOperationException("conflict!"),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("MAPPED");
        result.Errors[0].Type.Should().Be(ErrorType.Conflict);
    }
}
