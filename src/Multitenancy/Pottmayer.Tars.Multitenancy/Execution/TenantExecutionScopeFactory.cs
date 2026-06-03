using Pottmayer.Tars.Multitenancy.Abstractions.Context;
using Pottmayer.Tars.Multitenancy.Abstractions.Execution;

namespace Pottmayer.Tars.Multitenancy.Execution;

/// <summary>
/// Creates a disposable scope that sets <see cref="ITenantContextAccessor.Current"/> for the duration.
/// On disposal restores the previous context. Used for jobs, hosted services and tests.
/// </summary>
public sealed class TenantExecutionScopeFactory : ITenantExecutionScopeFactory
{
    private readonly ITenantContextAccessor _accessor;

    public TenantExecutionScopeFactory(ITenantContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    public ValueTask<IAsyncDisposable> BeginAsync(
        ITenantContext tenantContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);
        var previous = _accessor.Current;
        _accessor.SetCurrent(tenantContext);
        IAsyncDisposable scope = new TenantScope(_accessor, previous);
        return ValueTask.FromResult(scope);
    }

    private sealed class TenantScope : IAsyncDisposable
    {
        private readonly ITenantContextAccessor _accessor;
        private readonly ITenantContext? _previous;
        private bool _disposed;

        public TenantScope(ITenantContextAccessor accessor, ITenantContext? previous)
        {
            _accessor = accessor;
            _previous = previous;
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _accessor.SetCurrent(_previous);
                _disposed = true;
            }
            return ValueTask.CompletedTask;
        }
    }
}
