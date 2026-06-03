using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.Queries;

/// <summary>
/// Marker for a query that returns <see cref="Result{TResult}"/>.
/// </summary>
public interface IQuery<TResult> : IRequest<Result<TResult>>
    where TResult : notnull
{
    IQueryOptions QueryOptions { get; init; }
}

/// <summary>
/// Query with explicit input that returns <see cref="Result{TResult}"/>.
/// </summary>
public interface IQuery<TInput, TResult> : IQuery<TResult>
    where TInput : notnull
    where TResult : notnull
{
    TInput Input { get; }
}
