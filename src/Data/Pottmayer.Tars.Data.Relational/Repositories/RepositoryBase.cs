using System.Data;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using RelationalDataContext = Pottmayer.Tars.Data.Relational.DataContext.DataContext;

namespace Pottmayer.Tars.Data.Relational.Repositories;

/// <summary>
/// Base class for all application repositories using the relational stack.
/// Resolves the ambient <see cref="RelationalDataContext"/> from <see cref="IDataContextAccessor"/>.
/// <para>
/// Exposes <see cref="DbContext"/> for EF Core operations and <see cref="Connection"/> for
/// raw Dapper queries — both share the same underlying connection and transaction.
/// </para>
/// </summary>
public abstract class RepositoryBase
{
    private readonly RelationalDataContext _context;

    protected RepositoryBase(IDataContextAccessor accessor)
    {
        _context = (accessor ?? throw new ArgumentNullException(nameof(accessor))).Current as RelationalDataContext
            ?? throw new InvalidOperationException(
                "No ambient DataContext found. Ensure the repository is accessed via IUnitOfWork or IDataContextFactory.");
    }

    /// <summary>EF Core DbContext for change-tracked operations.</summary>
    protected RelationalDbContext DbContext => _context.DbContext;

    /// <summary>Shared database connection for Dapper queries in the same transaction.</summary>
    protected IDbConnection Connection => _context.Connection;

    protected void CollectDomainEvents(IHasDomainEvents aggregate)
        => _context.CollectDomainEvents(aggregate);
}
