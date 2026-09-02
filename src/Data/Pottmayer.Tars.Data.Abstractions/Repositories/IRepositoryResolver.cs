namespace Pottmayer.Tars.Data.Abstractions.Repositories;

/// <summary>Resolves repository instances bound to a data context.</summary>
public interface IRepositoryResolver
{
    /// <summary>Resolves a repository of the given type.</summary>
    /// <typeparam name="TRepository">The repository interface to resolve.</typeparam>
    /// <returns>The resolved repository.</returns>
    TRepository ResolveRepository<TRepository>() where TRepository : class, IRepository;

    /// <summary>Resolves a repository by its runtime type.</summary>
    /// <param name="repositoryType">The repository type to resolve.</param>
    /// <returns>The resolved repository.</returns>
    IRepository ResolveRepository(Type repositoryType);
}
