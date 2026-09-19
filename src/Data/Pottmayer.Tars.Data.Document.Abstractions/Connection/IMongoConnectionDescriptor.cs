namespace Pottmayer.Tars.Data.Document.Abstractions.Connection;

/// <summary>
/// Describes a resolved MongoDB connection: the connection string, the target database name and tenant info.
/// The document axis has no relational <c>DbProvider</c>; a database <b>name</b> takes the place a schema/catalog
/// would in the relational world.
/// </summary>
public interface IMongoConnectionDescriptor
{
    /// <summary>Key identifying the logical database this connection belongs to.</summary>
    string DatabaseKey { get; }

    /// <summary>The resolved MongoDB connection string (e.g. <c>mongodb://host:27017</c>).</summary>
    string ConnectionString { get; }

    /// <summary>The MongoDB database name to open on the connection.</summary>
    string DatabaseName { get; }

    /// <summary>Whether the connection is scoped to a specific tenant.</summary>
    bool IsTenantScoped { get; }

    /// <summary>The tenant this connection belongs to, when <see cref="IsTenantScoped"/> is true.</summary>
    string? TenantKey { get; }

    /// <summary>Additional resolver-specific metadata.</summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }
}
