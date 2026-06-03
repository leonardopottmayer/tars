using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.Commands;

/// <summary>
/// Marker for a command that returns <see cref="Result{TResult}"/>.
/// </summary>
public interface ICommand<TResult> : IRequest<Result<TResult>>
    where TResult : notnull
{
    ICommandOptions CommandOptions { get; set; }
}

/// <summary>
/// Command with explicit input that returns <see cref="Result{TResult}"/>.
/// </summary>
public interface ICommand<TInput, TResult> : ICommand<TResult>
    where TInput : notnull
    where TResult : notnull
{
    TInput Input { get; }
}
