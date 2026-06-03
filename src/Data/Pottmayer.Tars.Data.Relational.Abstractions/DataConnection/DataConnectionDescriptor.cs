using Pottmayer.Tars.Data.Relational.Abstractions.Enums;

namespace Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;

public sealed class DataConnectionDescriptor : IDataConnectionDescriptor
{
    public required string DatabaseKey { get; init; }
    public required string ConnectionString { get; init; }
    public DbProvider Provider { get; init; }
    public bool IsTenantScoped { get; init; }
    public string? TenantKey { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}
