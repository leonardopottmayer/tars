using MongoDB.Driver;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Document.MongoDB.DataContext;

namespace Pottmayer.Tars.Data.Document.MongoDB.Repositories;

/// <summary>
/// Base class for all application repositories using the MongoDB document stack.
/// Resolves the ambient <see cref="MongoDataContext"/> from <see cref="IDataContextAccessor"/> and exposes
/// the database plus the shared session/transaction so every operation joins the same unit of work.
/// </summary>
public abstract class MongoRepositoryBase
{
    private readonly MongoDataContext _context;

    /// <summary>Resolves and captures the ambient data context for this repository.</summary>
    /// <param name="accessor">Accessor providing the ambient data context.</param>
    /// <exception cref="InvalidOperationException">No ambient MongoDB data context is present.</exception>
    protected MongoRepositoryBase(IDataContextAccessor accessor)
    {
        _context = (accessor ?? throw new ArgumentNullException(nameof(accessor))).Current as MongoDataContext
            ?? throw new InvalidOperationException(
                "No ambient MongoDataContext found. Ensure the repository is accessed via IUnitOfWork or IDataContextFactory " +
                "for a database key registered with AddTarsMongoData.");
    }

    /// <summary>The MongoDB database for this unit of work.</summary>
    protected IMongoDatabase Database => _context.Database;

    /// <summary>Returns the shared session (with its open transaction) that every operation must use.</summary>
    protected ValueTask<IClientSessionHandle> SessionAsync(CancellationToken cancellationToken = default)
        => _context.GetSessionAsync(cancellationToken);

    /// <summary>Registers an aggregate whose domain events should be collected and dispatched on commit.</summary>
    /// <param name="aggregate">The aggregate that was added, updated or removed.</param>
    protected void Track(IHasDomainEvents aggregate) => _context.Track(aggregate);

    /// <summary>Registers domain events from an aggregate, draining them immediately into the manual buffer.</summary>
    /// <param name="aggregate">The aggregate whose pending domain events should be collected.</param>
    protected void CollectDomainEvents(IHasDomainEvents aggregate) => _context.CollectDomainEvents(aggregate);
}
