using Pottmayer.Tars.Multitenancy.Abstractions.Context;

namespace Pottmayer.Tars.Multitenancy.Abstractions.Execution;

/// <summary>
/// Creates an ambient tenant execution scope without requiring HTTP context.
/// Suitable for jobs, hosted services, tests and CLI tools.
/// </summary>
public interface ITenantExecutionScopeFactory
{
    /// <summary>
    /// Begins a scope where <see cref="ITenantContextAccessor.Current"/> returns the given <paramref name="tenantContext"/>.
    /// Dispose the returned object to clear the scope.
    /// </summary>
    ValueTask<IAsyncDisposable> BeginAsync(
        ITenantContext tenantContext,
        CancellationToken cancellationToken = default);
}
