namespace Pottmayer.Tars.Core.Cqrs.Queries
{
    /// <summary>Default <see cref="IQueryOptions"/> implementation.</summary>
    public class QueryOptions : IQueryOptions
    {
        /// <summary>Initializes a new instance with default settings.</summary>
        public QueryOptions() { }

        /// <summary>Creates a new instance with default settings.</summary>
        /// <returns>A new <see cref="QueryOptions"/>.</returns>
        public static QueryOptions New() => new();

        /// <summary>Returns the default options used when a query specifies none.</summary>
        /// <returns>A new <see cref="QueryOptions"/>.</returns>
        public static QueryOptions Default() => New();
    }
}
