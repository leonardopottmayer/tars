using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http;

/// <summary>
/// Default implementation of <see cref="IWrapDecisionService"/>.
/// </summary>
public sealed class WrapDecisionService : IWrapDecisionService
{
    /// <inheritdoc/>
    public bool ShouldWrap(WrapDecisionContext context)
    {
        if (!context.WrappingEnabled)
            return false;

        if (context.IsFileOrStream || context.IsAlreadyWrapped)
            return false;

        if (context.IsExplicitDisabled)
            return false;

        if (context.MinimalApiOptIn)
            return true;

        if (context.IsExplicitEnabled)
            return true;

        return context.ControllersDefaultMode == ControllersWrappingMode.WrapAll;
    }
}
