namespace Pottmayer.Tars.Multitenancy.Abstractions.Context;

/// <summary>
/// Holds the ambient <see cref="ITenantContext"/> for the current async execution flow.
/// Backed by <see cref="AsyncLocal{T}"/> so it flows correctly across awaits.
/// </summary>
public interface ITenantContextAccessor
{
    ITenantContext? Current { get; }
    void SetCurrent(ITenantContext? context);
}
