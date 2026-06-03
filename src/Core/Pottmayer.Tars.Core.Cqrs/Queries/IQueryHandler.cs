using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.Queries;

/// <summary>
/// Handler for <see cref="IQuery{TResult}"/> that returns <see cref="Result{TResult}"/>.
/// </summary>
public interface IQueryHandler<TQuery, TResult> : IRequestHandler<TQuery, Result<TResult>>
    where TQuery : IRequest<Result<TResult>>
    where TResult : notnull
{ }
