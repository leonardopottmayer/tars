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
    /// <summary>The ambient non-keyed data context for the current async flow, if any.</summary>
    IDataContext? Current { get; }

    /// <summary>Sets (or clears, when null) the ambient non-keyed data context.</summary>
    /// <param name="context">The context to make current, or null to clear it.</param>
    void SetCurrent(IDataContext? context);

    /// <summary>Gets the ambient data context tracked under <paramref name="databaseKey"/>, if any.</summary>
    /// <param name="databaseKey">The database key identifying the context.</param>
    /// <returns>The current context for the key, or null when none is set.</returns>
    IDataContext? GetCurrent(string databaseKey);

    /// <summary>Sets (or clears, when null) the ambient data context for <paramref name="databaseKey"/>.</summary>
    /// <param name="databaseKey">The database key identifying the context.</param>
    /// <param name="context">The context to make current, or null to clear it.</param>
    void SetCurrent(string databaseKey, IDataContext? context);
}
