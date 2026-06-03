using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Repositories;

namespace Pottmayer.Tars.Data.Relational.Repositories;

/// <summary>
/// Resolves repositories from DI, temporarily setting the ambient context so
/// the repository's constructor (or base-class accessor) picks it up correctly.
/// </summary>
internal sealed class RepositoryResolver : IRepositoryResolver
{
    private readonly IDataContextAccessor _accessor;
    private readonly IDataContext _context;
    private readonly IServiceProvider _serviceProvider;

    public RepositoryResolver(IDataContextAccessor accessor, IDataContext context, IServiceProvider serviceProvider)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public TRepository ResolveRepository<TRepository>() where TRepository : class, IRepository
        => (TRepository)ResolveRepository(typeof(TRepository));

    public IRepository ResolveRepository(Type repositoryType)
    {
        var previous = _accessor.Current;
        _accessor.SetCurrent(_context);
        try
        {
            return (IRepository)(_serviceProvider.GetService(repositoryType)
                ?? throw new InvalidOperationException(
                    $"Repository '{repositoryType.Name}' is not registered. " +
                    $"Call services.AddTarsDataRepositoriesFromAssemblies(...) or register it manually."));
        }
        finally
        {
            _accessor.SetCurrent(previous);
        }
    }
}
