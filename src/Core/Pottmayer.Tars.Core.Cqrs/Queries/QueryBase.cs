namespace Pottmayer.Tars.Core.Cqrs.Queries
{
    /// <summary>Base class for a query that returns <see cref="Primitives.Outcomes.Result{TResult}"/>.</summary>
    /// <typeparam name="TResult">Type carried by a successful result.</typeparam>
    public abstract class QueryBase<TResult> : IQuery<TResult>
        where TResult : notnull
    {
        /// <inheritdoc/>
        public IQueryOptions QueryOptions { get; init; } = Queries.QueryOptions.Default();

        /// <summary>Initializes a new query with default options.</summary>
        protected QueryBase() { }
    }

    /// <summary>Base class for a query with an explicit input that returns <see cref="Primitives.Outcomes.Result{TResult}"/>.</summary>
    /// <typeparam name="TInput">Type of the query input payload.</typeparam>
    /// <typeparam name="TResult">Type carried by a successful result.</typeparam>
    public abstract class QueryBase<TInput, TResult> : QueryBase<TResult>
        where TInput : notnull
        where TResult : notnull
    {
        /// <inheritdoc/>
        public TInput Input { get; }

        /// <summary>Initializes a new query with the given input and default options.</summary>
        /// <param name="input">The query input payload.</param>
        protected QueryBase(TInput input)
        {
            Input = input;
        }
    }
}
