namespace Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;

/// <summary>
/// Context passed to each <see cref="IDataConnectionResolver"/> when resolving a connection.
/// </summary>
public sealed class DataConnectionResolutionContext
{
    public required string DatabaseKey { get; init; }
    public string? TenantKey { get; init; }
    public string? TenantCode { get; init; }
    public required IServiceProvider ServiceProvider { get; init; }
}
