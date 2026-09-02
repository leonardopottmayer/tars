using Pottmayer.Tars.Data.Abstractions.UnitOfWork;

namespace Pottmayer.Tars.Data.Relational.Abstractions.MultiDb;

/// <summary>Execution context for multi-database work: hands out a unit of work per participating database.</summary>
public interface IMultiDatabaseExecutionContext
{
    /// <summary>Returns the unit of work for the given database key within this coordinated operation.</summary>
    /// <param name="databaseKey">Key identifying the participating database.</param>
    /// <returns>The unit of work bound to that database.</returns>
    IUnitOfWork GetUnitOfWork(string databaseKey);
}
