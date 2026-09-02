using Pottmayer.Tars.Data.Relational.Abstractions.Enums;

namespace Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;

/// <summary>
/// Describes a resolved database connection, including provider, connection string and tenant info.
/// </summary>
public interface IDataConnectionDescriptor
{
    /// <summary>Key identifying the logical database this connection belongs to.</summary>
    string DatabaseKey { get; }

    /// <summary>The resolved connection string.</summary>
    string ConnectionString { get; }

    /// <summary>The database provider for this connection.</summary>
    DbProvider Provider { get; }

    /// <summary>Whether the connection is scoped to a specific tenant.</summary>
    bool IsTenantScoped { get; }

    /// <summary>The tenant this connection belongs to, when <see cref="IsTenantScoped"/> is true.</summary>
    string? TenantKey { get; }

    /// <summary>Additional provider- or resolver-specific metadata.</summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }
}
