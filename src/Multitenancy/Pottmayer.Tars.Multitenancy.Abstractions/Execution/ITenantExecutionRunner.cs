using Pottmayer.Tars.Multitenancy.Abstractions.Context;

namespace Pottmayer.Tars.Multitenancy.Abstractions.Execution;

/// <summary>
/// Executes work within a tenant's context scope.
/// Used by hosted services and jobs that need tenant-aware data access without HTTP.
/// </summary>
public interface ITenantExecutionRunner
{
    /// <summary>
    /// Creates a DI scope, sets the tenant context, executes the delegate, then clears the context.
    /// </summary>
    Task RunForTenantAsync(
        ITenantContext tenantContext,
        Func<IServiceProvider, CancellationToken, Task> work,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Iterates over all tenant contexts and calls <see cref="RunForTenantAsync"/> for each.
    /// Respects <see cref="TenantExecutionOptions.MaxDegreeOfParallelism"/>.
    /// </summary>
    Task RunForEachTenantAsync(
        IAsyncEnumerable<ITenantContext> tenants,
        Func<IServiceProvider, ITenantContext, CancellationToken, Task> work,
        TenantExecutionOptions? options = null,
        CancellationToken cancellationToken = default);
}
