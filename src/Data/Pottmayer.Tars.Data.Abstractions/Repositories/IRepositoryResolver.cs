namespace Pottmayer.Tars.Data.Abstractions.Repositories;

public interface IRepositoryResolver
{
    TRepository ResolveRepository<TRepository>() where TRepository : class, IRepository;
    IRepository ResolveRepository(Type repositoryType);
}
