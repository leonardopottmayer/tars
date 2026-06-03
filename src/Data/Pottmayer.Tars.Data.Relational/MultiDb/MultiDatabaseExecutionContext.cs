using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Data.Relational.Abstractions.MultiDb;

namespace Pottmayer.Tars.Data.Relational.MultiDb;

internal sealed class MultiDatabaseExecutionContext : IMultiDatabaseExecutionContext
{
    private readonly Dictionary<string, IUnitOfWork> _units;

    public MultiDatabaseExecutionContext(IReadOnlyList<string> databaseKeys, IUnitOfWorkFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _units = new Dictionary<string, IUnitOfWork>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in databaseKeys)
            _units[key] = factory.Create(key);
    }

    public IUnitOfWork GetUnitOfWork(string databaseKey)
    {
        if (_units.TryGetValue(databaseKey, out var uow))
            return uow;
        throw new KeyNotFoundException($"Database key '{databaseKey}' is not part of this multi-database execution.");
    }

    public IEnumerable<IUnitOfWork> AllUnits => _units.Values;
}
