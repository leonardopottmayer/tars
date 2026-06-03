using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Multitenancy.Abstractions.Context;
using Pottmayer.Tars.Multitenancy.Abstractions.Execution;

namespace Pottmayer.Tars.Multitenancy.Execution;

/// <summary>
/// Default implementation of <see cref="ITenantExecutionRunner"/>.
/// Creates a fresh DI scope per tenant, sets the ambient tenant context, and runs the delegate.
/// </summary>
public sealed class TenantExecutionRunner : ITenantExecutionRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantContextAccessor _accessor;

    public TenantExecutionRunner(IServiceProvider serviceProvider, ITenantContextAccessor accessor)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    public async Task RunForTenantAsync(
        ITenantContext tenantContext,
        Func<IServiceProvider, CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);
        ArgumentNullException.ThrowIfNull(work);

        await using var scope = _serviceProvider.CreateAsyncScope();
        var previous = _accessor.Current;
        _accessor.SetCurrent(tenantContext);
        try
        {
            await work(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _accessor.SetCurrent(previous);
        }
    }

    public async Task RunForEachTenantAsync(
        IAsyncEnumerable<ITenantContext> tenants,
        Func<IServiceProvider, ITenantContext, CancellationToken, Task> work,
        TenantExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenants);
        ArgumentNullException.ThrowIfNull(work);

        var parallelism = options?.MaxDegreeOfParallelism ?? 1;
        if (parallelism <= 1)
        {
            await foreach (var tenant in tenants.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await RunForTenantAsync(
                    tenant,
                    (sp, ct) => work(sp, tenant, ct),
                    cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            var semaphore = new SemaphoreSlim(parallelism, parallelism);
            var tasks = new List<Task>();

            await foreach (var tenant in tenants.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                var capturedTenant = tenant;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await RunForTenantAsync(
                            capturedTenant,
                            (sp, ct) => work(sp, capturedTenant, ct),
                            cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
