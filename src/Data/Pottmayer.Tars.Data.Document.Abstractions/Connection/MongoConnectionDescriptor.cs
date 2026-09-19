namespace Pottmayer.Tars.Data.Document.Abstractions.Connection;

/// <summary>Default <see cref="IMongoConnectionDescriptor"/> implementation.</summary>
public sealed class MongoConnectionDescriptor : IMongoConnectionDescriptor
{
    /// <inheritdoc/>
    public required string DatabaseKey { get; init; }

    /// <inheritdoc/>
    public required string ConnectionString { get; init; }

    /// <inheritdoc/>
    public required string DatabaseName { get; init; }

    /// <inheritdoc/>
    public bool IsTenantScoped { get; init; }

    /// <inheritdoc/>
    public string? TenantKey { get; init; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}
