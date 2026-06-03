namespace Pottmayer.Tars.Core.Cqrs.Queries
{
    public class QueryOptions : IQueryOptions
    {
        public QueryOptions() { }

        public static QueryOptions New() => new();

        public static QueryOptions Default() => New();
    }
}
