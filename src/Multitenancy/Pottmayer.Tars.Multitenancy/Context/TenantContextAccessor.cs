using Pottmayer.Tars.Multitenancy.Abstractions.Context;

namespace Pottmayer.Tars.Multitenancy.Context;

/// <summary>
/// Holds the current ambient <see cref="ITenantContext"/> using <see cref="AsyncLocal{T}"/>
/// so it flows correctly across awaits within the same async execution context.
/// </summary>
public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private readonly AsyncLocal<ITenantContext?> _current = new();

    /// <inheritdoc/>
    public ITenantContext? Current => _current.Value;

    /// <inheritdoc/>
    public void SetCurrent(ITenantContext? context)
    {
        _current.Value = context;
    }
}
