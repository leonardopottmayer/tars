using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.Commands;

/// <summary>
/// Marker for a command that returns <see cref="Result{TResult}"/>.
/// </summary>
public interface ICommand<TResult> : IRequest<Result<TResult>>
    where TResult : notnull
{
    /// <summary>Behavioral options attached to this command.</summary>
    ICommandOptions CommandOptions { get; set; }
}

/// <summary>
/// Command with explicit input that returns <see cref="Result{TResult}"/>.
/// </summary>
public interface ICommand<TInput, TResult> : ICommand<TResult>
    where TInput : notnull
    where TResult : notnull
{
    /// <summary>The command's input payload.</summary>
    TInput Input { get; }
}
