namespace Pottmayer.Tars.UserContext.Abstractions;

/// <summary>
/// Provides read/write access to the current user context.
/// Intended to be set by host adapters (middleware, test harness, worker setup).
/// </summary>
public interface IUserContextAccessor
{
    /// <summary>The current user context; null when none has been set (e.g. anonymous or unset).</summary>
    IUserContext? Current { get; set; }
}
