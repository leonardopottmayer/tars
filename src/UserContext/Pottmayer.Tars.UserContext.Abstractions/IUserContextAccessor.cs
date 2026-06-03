namespace Pottmayer.Tars.UserContext.Abstractions;

/// <summary>
/// Provides read/write access to the current user context.
/// Intended to be set by host adapters (middleware, test harness, worker setup).
/// </summary>
public interface IUserContextAccessor
{
    IUserContext? Current { get; set; }
}
