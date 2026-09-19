using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Repositories;

namespace Pottmayer.Tars.Data.Repositories;

/// <summary>
/// Resolves repositories from DI, temporarily setting the ambient context so the repository's
/// constructor (or base-class accessor) picks it up correctly. Provider-agnostic — used by every
/// <see cref="IDataContext"/> implementation (relational, document, …).
/// </summary>
public sealed class RepositoryResolver : IRepositoryResolver
{
    private readonly IDataContextAccessor _accessor;
    private readonly IDataContext _context;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Creates a resolver bound to the given context.</summary>
    /// <param name="accessor">Accessor whose ambient <see cref="IDataContextAccessor.Current"/> is set during resolution.</param>
    /// <param name="context">The context repositories will be bound to.</param>
    /// <param name="serviceProvider">Service provider repositories are resolved from.</param>
    public RepositoryResolver(IDataContextAccessor accessor, IDataContext context, IServiceProvider serviceProvider)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public TRepository ResolveRepository<TRepository>() where TRepository : class, IRepository
        => (TRepository)ResolveRepository(typeof(TRepository));

    /// <inheritdoc/>
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
