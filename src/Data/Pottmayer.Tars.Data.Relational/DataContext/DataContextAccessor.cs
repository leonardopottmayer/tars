using Pottmayer.Tars.Data.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.Relational.DataContext;

/// <summary>
/// Async-local storage for ambient data contexts.
/// Non-keyed slot is used by RepositoryResolver during DI resolution;
/// keyed slots track concurrent database contexts.
/// </summary>
public sealed class DataContextAccessor : IDataContextAccessor
{
    private readonly AsyncLocal<IDataContext?> _current = new();
    private readonly AsyncLocal<Dictionary<string, IDataContext?>?> _keyed = new();

    public IDataContext? Current => _current.Value;

    public void SetCurrent(IDataContext? context) => _current.Value = context;

    public IDataContext? GetCurrent(string databaseKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseKey);
        var dict = _keyed.Value;
        return dict is not null && dict.TryGetValue(databaseKey, out var ctx) ? ctx : null;
    }

    public void SetCurrent(string databaseKey, IDataContext? context)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseKey);
        _keyed.Value ??= new Dictionary<string, IDataContext?>(StringComparer.OrdinalIgnoreCase);
        if (context is null)
            _keyed.Value.Remove(databaseKey);
        else
            _keyed.Value[databaseKey] = context;
    }
}
