namespace Pottmayer.Tars.Data.Abstractions.DataContext;

/// <summary>
/// Contract for a single registered pipeline that owns one <c>databaseKey</c>.
/// Each data provider (relational, document, …) contributes one implementation per key;
/// the composite <see cref="IDataContextFactory"/> dispatches to the correct instance by key.
/// This is the seam that lets multiple providers coexist behind a single
/// <see cref="Pottmayer.Tars.Data.Abstractions.UnitOfWork.IUnitOfWorkFactory"/>.
/// </summary>
public interface IKeyedDataContextFactory
{
    /// <summary>The database key this pipeline owns.</summary>
    string DatabaseKey { get; }

    /// <summary>Creates (or borrows) the ambient context for this key in the current async flow.</summary>
    Task<IDataContext> CreateScopedAsync(CancellationToken cancellationToken = default);

    /// <summary>Always creates a fresh context not tied to the ambient scope.</summary>
    Task<IDataContext> CreateIsolatedAsync(CancellationToken cancellationToken = default);
}
