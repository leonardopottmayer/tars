namespace Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;

public interface IDataConnectionResolver
{
    Task<IDataConnectionDescriptor?> ResolveAsync(
        DataConnectionResolutionContext context,
        CancellationToken cancellationToken = default);
}
