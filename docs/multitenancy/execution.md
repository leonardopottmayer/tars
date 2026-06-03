# Per-Tenant Execution

## Problem it solves

In workers, hosted services and jobs, there is no HTTP request that triggers automatic tenant resolution. `ITenantExecutionRunner` and `ITenantExecutionScopeFactory` provide the infrastructure to run work with the correct tenant context in any host.

---

## `ITenantExecutionRunner`

Runs work inside an isolated DI scope per tenant. For each tenant it:

1. Creates a new `IServiceScope`
2. Sets `ITenantContextAccessor.Current` with the tenant's context
3. Runs the delegate
4. Restores the previous context and disposes the scope

Registered as `Scoped` by `AddTarsMultitenancy()`.

### Run for a specific tenant

```csharp
public class TenantReportJob
{
    private readonly ITenantExecutionRunner _runner;
    private readonly ITenantCatalog _catalog;

    public TenantReportJob(ITenantExecutionRunner runner, ITenantCatalog catalog)
    {
        _runner = runner;
        _catalog = catalog;
    }

    public async Task RunAsync(string tenantKey, CancellationToken ct)
    {
        var tenantContext = TenantContext.Create(tenantKey);

        await _runner.RunForTenantAsync(
            tenantContext,
            async (sp, token) =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                await mediator.Send(new GenerateReportCommand(), token);
            },
            ct);
    }
}
```

### Iterate over all tenants (sequential)

```csharp
public class DailyMaintenanceJob
{
    private readonly ITenantExecutionRunner _runner;
    private readonly ITenantCatalog _catalog;

    public DailyMaintenanceJob(ITenantExecutionRunner runner, ITenantCatalog catalog)
    {
        _runner = runner;
        _catalog = catalog;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await _runner.RunForEachTenantAsync(
            _catalog.ListAsync(ct),
            async (sp, tenantCtx, token) =>
            {
                var svc = sp.GetRequiredService<IMaintenanceService>();
                await svc.RunDailyCleanupAsync(token);
            },
            cancellationToken: ct);
    }
}
```

### Iterate in parallel

Increase `MaxDegreeOfParallelism` to process multiple tenants simultaneously. Each tenant gets its own DI scope and an isolated ambient context via `AsyncLocal`.

```csharp
await _runner.RunForEachTenantAsync(
    _catalog.ListAsync(ct),
    async (sp, tenantCtx, token) =>
    {
        var svc = sp.GetRequiredService<ISyncService>();
        await svc.SyncAsync(token);
    },
    new TenantExecutionOptions { MaxDegreeOfParallelism = 4 },
    ct);
```

> **Caution with parallelism**: each parallel worker has its own `AsyncLocal`, so the tenant context does not leak between them. However, shared resources (e.g. a queue, a distributed lock) still need explicit handling.

---

## `ITenantExecutionScopeFactory`

A lower-level execution scope. Useful when you need manual control of the start and end of the scope without using the runner. Common in tests and in integrations with queue frameworks.

```csharp
public class QueueMessageHandler
{
    private readonly ITenantExecutionScopeFactory _scopeFactory;
    private readonly ITenantContextFactory _contextFactory;

    public QueueMessageHandler(
        ITenantExecutionScopeFactory scopeFactory,
        ITenantContextFactory contextFactory)
    {
        _scopeFactory = scopeFactory;
        _contextFactory = contextFactory;
    }

    public async Task HandleAsync(QueueMessage message, CancellationToken ct)
    {
        var resolutionResult = TenantResolutionResult.Resolved(message.TenantKey);
        var tenantContext = _contextFactory.Create(resolutionResult);

        await using var scope = await _scopeFactory.BeginAsync(tenantContext, ct);

        // During this block, ITenantContextAccessor.Current == tenantContext
        await ProcessMessageAsync(message, ct);

        // scope.DisposeAsync() restores the previous context automatically
    }
}
```

---

## Worker Service with multitenancy

Complete example of an `IHostedService` that processes all tenants in a loop:

```csharp
public class TenantSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TenantSyncWorker> _logger;

    public TenantSyncWorker(IServiceProvider services, ILogger<TenantSyncWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = _services.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<ITenantExecutionRunner>();
            var catalog = scope.ServiceProvider.GetRequiredService<ITenantCatalog>();

            await runner.RunForEachTenantAsync(
                catalog.ListAsync(stoppingToken),
                async (sp, tenantCtx, ct) =>
                {
                    _logger.LogInformation("Syncing tenant {Key}", tenantCtx.TenantKey);
                    var svc = sp.GetRequiredService<ISyncService>();
                    await svc.SyncAsync(ct);
                },
                new TenantExecutionOptions { MaxDegreeOfParallelism = 2 },
                stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

Worker registration:

```csharp
builder.Services.AddHostedService<TenantSyncWorker>();
builder.Services.AddTarsMultitenancy();
builder.Services.AddTarsInMemoryTenantCatalog(["acme", "globex"]);
```

---

## Setting the tenant manually (without the runner)

For simple scenarios where the tenant is known before execution and a separate DI scope is not needed:

```csharp
var accessor = serviceProvider.GetRequiredService<ITenantContextAccessor>();
var context = TenantContext.Create("acme");
accessor.SetCurrent(context);

// From here on, any service that injects ITenantContext receives the "acme" context
await doWork();

// Clear after use in non-web scenarios
accessor.SetCurrent(null);
```

---

## `TenantContext` — static factories

```csharp
// From a simple key
TenantContext.Create("acme");

// With a different code
TenantContext.Create("acme", tenantCode: "ACME");

// With extra properties
TenantContext.Create("acme", properties: new Dictionary<string, object?>
{
    ["plan"] = "enterprise",
    ["region"] = "us-east-1"
});

// Empty context (unresolved)
TenantContext.Unresolved();

// From a resolution result
TenantContext.FromResolution(resolutionResult);
```
