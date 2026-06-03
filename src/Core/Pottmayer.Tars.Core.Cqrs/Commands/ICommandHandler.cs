using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.Commands;

/// <summary>
/// Handler for <see cref="ICommand{TResult}"/> that returns <see cref="Result{TResult}"/>.
/// </summary>
public interface ICommandHandler<TCommand, TResult> : IRequestHandler<TCommand, Result<TResult>>
    where TCommand : IRequest<Result<TResult>>
    where TResult : notnull
{ }