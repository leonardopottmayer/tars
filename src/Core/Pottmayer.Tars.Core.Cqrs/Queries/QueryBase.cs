namespace Pottmayer.Tars.Core.Cqrs.Queries
{
    public abstract class QueryBase<TResult> : IQuery<TResult>
        where TResult : notnull
    {
        public IQueryOptions QueryOptions { get; init; } = Queries.QueryOptions.Default();

        protected QueryBase() { }
    }

    public abstract class QueryBase<TInput, TResult> : QueryBase<TResult>
        where TInput : notnull
        where TResult : notnull
    {
        public TInput Input { get; }

        protected QueryBase(TInput input)
        {
            Input = input;
        }
    }
}
