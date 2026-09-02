using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.Queries
{
    /// <summary>
    /// Base class for query handlers. Adapts the mediator's <see cref="Handle"/> entry point onto the
    /// async <see cref="HandleAsync"/> template method and offers helpers for building success/failure results.
    /// </summary>
    /// <typeparam name="TQuery">The query type handled.</typeparam>
    /// <typeparam name="TResult">Type carried by a successful result.</typeparam>
    public abstract class QueryHandlerBase<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
        where TResult : notnull
    {
        /// <summary>Initializes the handler base.</summary>
        protected QueryHandlerBase() { }

        /// <inheritdoc/>
        public ValueTask<Result<TResult>> Handle(TQuery request, CancellationToken cancellationToken = default)
        {
            return new ValueTask<Result<TResult>>(HandleAsync(request, cancellationToken));
        }

        /// <summary>Builds a successful result carrying <paramref name="value"/>.</summary>
        /// <param name="value">The value produced by the query.</param>
        /// <param name="correlationId">Optional correlation id to attach to the result.</param>
        /// <returns>A successful <see cref="Result{TResult}"/>.</returns>
        protected Result<TResult> Ok(TResult value, string? correlationId = null)
            => Result<TResult>.Success(value, correlationId);

        /// <summary>Builds a failed result from the given errors.</summary>
        /// <param name="errors">The errors describing the failure.</param>
        /// <returns>A failed <see cref="Result{TResult}"/>.</returns>
        protected Result<TResult> Fail(params Error[] errors)
            => Result<TResult>.Failure(errors);

        /// <summary>Builds a failed result from the given errors.</summary>
        /// <param name="errors">The errors describing the failure.</param>
        /// <param name="correlationId">Optional correlation id to attach to the result.</param>
        /// <returns>A failed <see cref="Result{TResult}"/>.</returns>
        protected Result<TResult> Fail(IEnumerable<Error> errors, string? correlationId = null)
            => Result<TResult>.Failure(errors, correlationId);

        /// <summary>Handles the query. Implemented by concrete handlers.</summary>
        /// <param name="request">The query to handle.</param>
        /// <param name="cancellationToken">Token used to cancel handling.</param>
        /// <returns>The outcome of handling the query.</returns>
        protected abstract Task<Result<TResult>> HandleAsync(TQuery request, CancellationToken cancellationToken);
    }
}
