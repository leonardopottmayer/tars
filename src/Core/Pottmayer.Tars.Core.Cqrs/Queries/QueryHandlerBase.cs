using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.Queries
{
    public abstract class QueryHandlerBase<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
        where TResult : notnull
    {
        protected QueryHandlerBase() { }

        public ValueTask<Result<TResult>> Handle(TQuery request, CancellationToken cancellationToken = default)
        {
            return new ValueTask<Result<TResult>>(HandleAsync(request, cancellationToken));
        }

        protected Result<TResult> Ok(TResult value, string? correlationId = null)
            => Result<TResult>.Success(value, correlationId);

        protected Result<TResult> Fail(params Error[] errors)
            => Result<TResult>.Failure(errors);

        protected Result<TResult> Fail(IEnumerable<Error> errors, string? correlationId = null)
            => Result<TResult>.Failure(errors, correlationId);

        protected abstract Task<Result<TResult>> HandleAsync(TQuery request, CancellationToken cancellationToken);
    }
}
