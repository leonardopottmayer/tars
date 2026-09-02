namespace Pottmayer.Tars.Web.Http.Abstractions;

/// <summary>
/// Defines the default response-wrapping behavior for MVC controllers.
/// </summary>
public enum ControllersWrappingMode
{
    /// <summary>Wraps controller responses unless explicitly disabled.</summary>
    WrapAll = 0,
    /// <summary>Leaves controller responses unwrapped unless explicitly enabled.</summary>
    WrapNone = 1
}
