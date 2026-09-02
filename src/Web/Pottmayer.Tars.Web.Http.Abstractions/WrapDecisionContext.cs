namespace Pottmayer.Tars.Web.Http.Abstractions;

/// <summary>
/// Supplies endpoint and response state to an <see cref="IWrapDecisionService"/>.
/// </summary>
public sealed class WrapDecisionContext
{
    /// <summary>Gets whether the response is a file or stream.</summary>
    public bool IsFileOrStream { get; init; }
    /// <summary>Gets whether the response is already wrapped.</summary>
    public bool IsAlreadyWrapped { get; init; }
    /// <summary>Gets whether wrapping was explicitly disabled.</summary>
    public bool IsExplicitDisabled { get; init; }
    /// <summary>Gets whether wrapping was explicitly enabled.</summary>
    public bool IsExplicitEnabled { get; init; }
    /// <summary>Gets the default MVC controller wrapping mode.</summary>
    public ControllersWrappingMode ControllersDefaultMode { get; init; }
    /// <summary>Gets whether the Minimal API endpoint opted in to wrapping.</summary>
    public bool MinimalApiOptIn { get; init; }
    /// <summary>Gets whether wrapping is enabled globally.</summary>
    public bool WrappingEnabled { get; init; } = true;
}
