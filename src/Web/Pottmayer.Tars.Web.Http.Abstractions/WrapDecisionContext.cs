namespace Pottmayer.Tars.Web.Http.Abstractions;

public sealed class WrapDecisionContext
{
    public bool IsFileOrStream { get; init; }
    public bool IsAlreadyWrapped { get; init; }
    public bool IsExplicitDisabled { get; init; }
    public bool IsExplicitEnabled { get; init; }
    public ControllersWrappingMode ControllersDefaultMode { get; init; }
    public bool MinimalApiOptIn { get; init; }
    public bool WrappingEnabled { get; init; } = true;
}
