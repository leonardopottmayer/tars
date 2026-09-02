using Pottmayer.Tars.Data.Relational.Abstractions.Enums;

namespace Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;

/// <summary>Default <see cref="IDataConnectionDescriptor"/> implementation.</summary>
public sealed class DataConnectionDescriptor : IDataConnectionDescriptor
{
    /// <inheritdoc/>
    public required string DatabaseKey { get; init; }

    /// <inheritdoc/>
    public required string ConnectionString { get; init; }

    /// <inheritdoc/>
    public DbProvider Provider { get; init; }

    /// <inheritdoc/>
    public bool IsTenantScoped { get; init; }

    /// <inheritdoc/>
    public string? TenantKey { get; init; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}
