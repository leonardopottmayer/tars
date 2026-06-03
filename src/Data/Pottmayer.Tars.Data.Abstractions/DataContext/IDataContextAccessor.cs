namespace Pottmayer.Tars.Data.Abstractions.DataContext;

/// <summary>
/// Provides ambient access to data contexts in the current async execution flow.
/// <list type="bullet">
///   <item><b>Non-keyed</b> (<see cref="Current"/> / <see cref="SetCurrent(IDataContext?)"/>):
///   used internally by <see cref="IRepositoryResolver"/> during DI resolution.</item>
///   <item><b>Keyed</b> (<see cref="GetCurrent"/> / <see cref="SetCurrent(string,IDataContext?)"/>):
///   used by context factories to track multiple concurrent databases.</item>
/// </list>
/// </summary>
public interface IDataContextAccessor
{
    IDataContext? Current { get; }
    void SetCurrent(IDataContext? context);

    IDataContext? GetCurrent(string databaseKey);
    void SetCurrent(string databaseKey, IDataContext? context);
}
