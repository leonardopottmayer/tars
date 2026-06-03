using Pottmayer.Tars.Data.Relational.Abstractions.Enums;

namespace Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;

/// <summary>
/// Describes a resolved database connection, including provider, connection string and tenant info.
/// </summary>
public interface IDataConnectionDescriptor
{
    string DatabaseKey { get; }
    string ConnectionString { get; }
    DbProvider Provider { get; }
    bool IsTenantScoped { get; }
    string? TenantKey { get; }
    IReadOnlyDictionary<string, object?> Metadata { get; }
}
