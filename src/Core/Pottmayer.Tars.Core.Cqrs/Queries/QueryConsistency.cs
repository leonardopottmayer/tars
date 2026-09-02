namespace Pottmayer.Tars.Core.Cqrs.Queries
{
    /// <summary>Consistency level a query expects from the underlying read model.</summary>
    public enum QueryConsistency
    {
        /// <summary>Use the read model's default consistency.</summary>
        Default = 0,

        /// <summary>Require strongly consistent (up-to-date) reads.</summary>
        Strong = 1,

        /// <summary>Accept eventually consistent (possibly stale) reads.</summary>
        Eventual = 2
    }
}
