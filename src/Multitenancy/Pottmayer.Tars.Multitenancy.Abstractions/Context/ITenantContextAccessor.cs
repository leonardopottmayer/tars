namespace Pottmayer.Tars.Multitenancy.Abstractions.Context;

/// <summary>
/// Holds the ambient <see cref="ITenantContext"/> for the current async execution flow.
/// Backed by <see cref="AsyncLocal{T}"/> so it flows correctly across awaits.
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>Gets the context for the current execution flow.</summary>
    ITenantContext? Current { get; }
    /// <summary>Sets the context for the current execution flow.</summary>
    /// <param name="context">The context to set, or <c>null</c> to clear it.</param>
    void SetCurrent(ITenantContext? context);
}
