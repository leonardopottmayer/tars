# Multitenancy Configuration

> Unlike other families, this module has **no `Tars:Multitenancy` bindable options section** and no
> `AddTarsMultitenancyOptions()` — deliberately. There is nothing to bind: which tenants exist, which
> resolver(s) run, and how connection strings map to tenants are all decisions the host makes in code
> (`AddTarsTenantResolution(options => ...)`, a custom `ITenantCatalog`), because they are usually
> derived from the host's own domain data, not static config. Any `appsettings` shown below (e.g.
> `Tenants:Known`) is an **app-owned** configuration class the scenario defines for itself, not a
> framework-bound options type.

## Base registration (any host)

The absolute minimum for the tenant context to work:

```csharp
using Pottmayer.Tars.Multitenancy.DI;

// 1. Register the ambient context services.
builder.Services.AddTarsTenantContextAccessor();
builder.Services.AddTarsTenantContextFactory();

// 2. Configure the resolution pipeline.
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver(new StaticTenantResolver("acme"));
});
```

The calls are shown in their recommended order, but order does not affect resolution because the
runtime services are factory-resolved. The registration methods use `TryAdd`; register a custom
implementation before its Tars registration when the custom implementation must win.

---

## Scenario 1 — Simple Web API, header resolution

```csharp
// Program.cs
builder.Services.AddTarsTenantContextAccessor();
builder.Services.AddTarsTenantContextFactory();
builder.Services.AddTarsHeaderTenantResolver("X-Tenant-Key");
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<HeaderTenantResolver>();
});

var app = builder.Build();
app.UseTarsTenantResolution();
app.MapControllers();
app.Run();
```

```json
// appsettings.json — no section required for this scenario
```

---

## Scenario 2 — Public app with subdomain, configuration catalog

```json
// appsettings.json
{
  "Tenants": {
    "Known": ["acme", "globex", "initech"]
  }
}
```

```csharp
// ConfigurationTenantCatalog.cs
public sealed class ConfigurationTenantCatalog : ITenantCatalog
{
    private readonly IReadOnlyList<string> _keys;

    public ConfigurationTenantCatalog(IConfiguration configuration)
    {
        _keys = configuration.GetSection("Tenants:Known").Get<string[]>() ?? [];
    }

    public async IAsyncEnumerable<ITenantContext> ListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var key in _keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return TenantContext.Create(key);
            await Task.Yield();
        }
    }
}
```

```csharp
// Program.cs
builder.Services.AddTarsTenantContextAccessor();
builder.Services.AddTarsTenantContextFactory();
builder.Services.AddTarsSubdomainTenantResolver();
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<SubdomainTenantResolver>();
});
builder.Services.AddTarsTenantCatalog<ConfigurationTenantCatalog>();

var app = builder.Build();
app.UseTarsTenantResolution();
app.MapControllers();
app.Run();
```

---

## Scenario 3 — SaaS with JWT, database store and full pipeline

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=MyApp;User Id=app;Password=pass;"
  }
}
```

```csharp
// Program.cs
builder.Services.AddTarsTenantContextAccessor();
builder.Services.AddTarsTenantContextFactory();
builder.Services.AddTarsTenantStore<DatabaseTenantStore>();
builder.Services.AddTarsTenantCatalog<DatabaseTenantCatalog>();

builder.Services.AddTarsTenantResolution(options =>
{
    // 1st: tenant from the JWT (logged-in user)
    options.AddResolver(new ClaimTenantResolver("tenant_key"));
    // 2nd: header for service-to-service calls
    options.AddResolver(new HeaderTenantResolver("X-Tenant-Key"));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* ... */);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseTarsTenantResolution();
app.MapControllers();
app.Run();
```

---

## Scenario 4 — Worker Service, all tenants in parallel

```json
// appsettings.json
{
  "Tenants": {
    "Known": ["acme", "globex", "initech"]
  },
  "TenantSync": {
    "IntervalMinutes": 5,
    "MaxParallelism": 3
  }
}
```

```csharp
// TenantSyncOptions.cs
public sealed class TenantSyncOptions
{
    public int IntervalMinutes { get; set; } = 5;
    public int MaxParallelism { get; set; } = 1;
}
```

```csharp
// TenantSyncWorker.cs
public class TenantSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly TenantSyncOptions _options;

    public TenantSyncWorker(IServiceProvider services, IOptions<TenantSyncOptions> options)
    {
        _services = services;
        _options = options.Value;
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
                    var svc = sp.GetRequiredService<ISyncService>();
                    await svc.SyncAsync(ct);
                },
                new TenantExecutionOptions { MaxDegreeOfParallelism = _options.MaxParallelism },
                stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
        }
    }
}
```

```csharp
// Program.cs
builder.Services.AddTarsTenantContextAccessor();
builder.Services.AddTarsTenantExecutionRunner();
builder.Services.AddTarsTenantCatalog<ConfigurationTenantCatalog>();
builder.Services.Configure<TenantSyncOptions>(builder.Configuration.GetSection("TenantSync"));
builder.Services.AddHostedService<TenantSyncWorker>();
builder.Services.AddScoped<ISyncService, SyncService>();
```

---

## Scenario 5 — Database isolation (database-per-tenant)

```json
// appsettings.json
{
  "Tenants": {
    "ConnectionStrings": {
      "acme": "Server=acme-db;Database=AcmeDb;User Id=app;Password=a;",
      "globex": "Server=globex-db;Database=GlobexDb;User Id=app;Password=b;"
    }
  }
}
```

```csharp
// Program.cs
builder.Services.AddTarsTenantContextAccessor();
builder.Services.AddTarsTenantContextFactory();
builder.Services.AddTarsHeaderTenantResolver("X-Tenant-Key");
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<HeaderTenantResolver>();
});
builder.Services.AddSingleton<ITenantConnectionStringProvider, ConfigurationTenantConnectionStringProvider>();
builder.Services.AddScoped<IDbContextFactory<AppDbContext>, TenantDbContextFactory>();
builder.Services.AddScoped<AppDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

var app = builder.Build();
app.UseTarsTenantResolution();
app.MapControllers();
app.Run();
```

---

## Scenario 6 — Schema isolation (schema-per-tenant)

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Default": "Server=shared-db;Database=MultiTenant;User Id=app;Password=pass;"
  },
  "Tenants": {
    "SchemaPrefix": "tenant_"
  }
}
```

```csharp
// Program.cs
builder.Services.AddTarsTenantContextAccessor();
builder.Services.AddTarsTenantContextFactory();
builder.Services.AddTarsHeaderTenantResolver("X-Tenant-Key");
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<HeaderTenantResolver>();
});
builder.Services.AddSingleton<ITenantSchemaProvider>(sp =>
{
    var prefix = sp.GetRequiredService<IConfiguration>()["Tenants:SchemaPrefix"] ?? "tenant_";
    return new PrefixedTenantSchemaProvider(prefix);
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();
app.UseTarsTenantResolution();
app.MapControllers();
app.Run();
```

---

## Quick reference of the DI methods

| Method | Package | Description |
|---|---|---|
| `AddTarsTenantContextAccessor()` | `Multitenancy` | Registers the ambient tenant context accessor |
| `AddTarsTenantContextFactory()` | `Multitenancy` | Registers the factory that creates contexts from resolution results |
| `AddTarsTenantExecutionScopeFactory()` | `Multitenancy` | Registers the ambient tenant execution scope factory |
| `AddTarsTenantExecutionRunner()` | `Multitenancy` | Registers the scoped per-tenant execution runner |
| `AddTarsTenantResolution(options => ...)` | `Multitenancy` | Registers the pipeline with the configured resolvers |
| `AddTarsInMemoryTenantCatalog(keys)` | `Multitenancy` | In-memory catalog with a fixed list of keys |
| `AddTarsTenantCatalog<T>()` | `Multitenancy` | Custom catalog |
| `AddTarsTenantStore<T>()` | `Multitenancy` | Custom store |
| `AddTarsHeaderTenantResolver(header)` | `Multitenancy.AspNetCore` | Registers `HeaderTenantResolver` as a singleton |
| `AddTarsSubdomainTenantResolver()` | `Multitenancy.AspNetCore` | Registers `SubdomainTenantResolver` as a singleton |
| `app.UseTarsTenantResolution()` | `Multitenancy.AspNetCore` | Adds the resolution middleware to the HTTP pipeline |

---

## What must not exist

```csharp
// DO NOT use — forbidden aggregator
services.AddTarsMultitenancy();
```

Registration is always composed by calling the individual methods above.
