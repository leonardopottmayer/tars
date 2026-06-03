using Pottmayer.Tars.Data.Abstractions.UnitOfWork;

namespace Pottmayer.Tars.Data.Relational.Abstractions.MultiDb;

public interface IMultiDatabaseExecutionContext
{
    IUnitOfWork GetUnitOfWork(string databaseKey);
}
