# Data, Multitenancy and Multi-Database

> This is the reference guide for combining **relational data access**, **multitenancy** and **multiple databases** in `Pottmayer.Tars`. It is the most complex topic in the framework, so the page is long and ends with several complete combinations.
>
> Related pages:
> - [Data — Configuration (Relational)](./configuration.md)
> - [Data — Contracts and UoW](./pipelines-and-uow.md)
> - [Multitenancy — Overview](../multitenancy/overview.md)
> - [Multitenancy — Configuration](../multitenancy/configuration.md)
> - [Multitenancy — Data isolation](../multitenancy/data-isolation.md)
> - [Multitenancy — Per-tenant execution](../multitenancy/execution.md)

---

## 1. What problem this solves

`Tars` is not limited to a single ambient database. The relational axis supports:

- single-database applications
- applications with a shared database + per-tenant databases
- applications with multiple shared databases
- applications with multiple per-tenant databases
- tenant resolution via HTTP
- execution outside HTTP (hosted services, jobs, manual scopes)

The central design idea is to separate **two independent questions**:

1. **Who is the current tenant?** → responsibility of the **multitenancy** layer.
2. **Which connection should be used for database key `X`?** → responsibility of the **data** layer.

Keeping these two concerns separate is what makes it possible to change the tenant strategy (header, subdomain, claim, job) without rewriting data access, and vice versa.

---

## 2. Mental model

### 2.1 Logical keys, not physical database names

The application code uses **logical database keys**:

- `default`
- `central`
- `shared`
- `primary`
- `secondary`

And **never** physical names like `roberto_central_local` or `app_dev_primary`. The framework resolves the logical key into a real connection string at runtime. This keeps the code environment-independent.

### 2.2 Tenant resolution and data resolution are separate

| Tenant resolution answers | Data resolution answers |
|---|---|
| the current tenant key (`TenantKey`) | connection string for a `databaseKey` |
| the tenant code (`TenantCode`) | which `DbProvider` to use |
| optional tenant metadata | whether the connection is tenant-scoped (`IsTenantScoped`) |

When `Pottmayer.Tars.Multitenancy` is active, the current tenant's `TenantKey`/`TenantCode` is passed automatically to data resolution via `DataConnectionResolutionContext`.

### 2.3 `default` is convenience, not mandatory architecture

`DataKeys.Default` is the string `"default"`. It is useful when the application wants a contextual "main" database. For apps with multiple database roles, **explicit keys** (`central`, `primary`, `secondary`) are clearer and avoid ambiguous routing.

---

## 3. Building blocks

### 3.1 Tenant abstractions (`Pottmayer.Tars.Multitenancy.Abstractions`)

- `ITenantContext`, `ITenantContextAccessor`, `ITenantContextFactory`
- `ITenantResolver`, the resolution pipeline and `TenantResolutionRequest` / `TenantResolutionResult`
- `ITenantCatalog`, `ITenantStore`
- `ITenantExecutionRunner`, `TenantExecutionOptions`

### 3.2 Data abstractions

Provider-agnostic (`Pottmayer.Tars.Data.Abstractions`):

- `IDataContext`, `IDataContextAccessor`
- `IUnitOfWork`, `IUnitOfWorkFactory`
- `IRepository` / `IRepository<T>`, `IRepositoryResolver`
- `DataKeys`

Specific to the relational axis (`Pottmayer.Tars.Data.Relational.Abstractions`):

- `IDataConnectionDescriptor`, `IDataConnectionResolver`, `DataConnectionResolutionContext`
- `IDataContextFactory` (`CreateScopedAsync` / `CreateIsolatedAsync`)
- `IMultiDatabaseCoordinator`, `IMultiDatabaseExecutionContext`
- `ITenantConnectionStringProvider`, `ITenantSchemaProvider` (namespace `...Relational.Abstractions.Multitenancy`)
- `DbProvider`

### 3.3 Per-key pipelines (`Pottmayer.Tars.Data.Relational`)

Each database is registered with `AddTarsData<TDbContext>`. For simple apps, the keyless overload uses `"default"`:

```csharp
services.AddTarsData<AppDbContext>((sp, d) =>
    new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(d.ConnectionString).Options);
```

For multi-database, one call per key:

```csharp
services.AddTarsData<CentralDbContext>("central", (sp, d) =>
    new DbContextOptionsBuilder<CentralDbContext>().UseNpgsql(d.ConnectionString).Options);
services.AddTarsData<TenantDbContext>("primary", (sp, d) =>
    new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(d.ConnectionString).Options);
```

> `TDbContext` must inherit from `RelationalDbContext`.

---

## 4. How connection resolution works

The `ConfigurationDataConnectionResolver` resolves a connection for a `databaseKey` in this **order of precedence**:

1. `Tars:Data:TenantConnections:{databaseKey}:{tenantKey}` — explicit per-tenant connection (only when there is a `TenantKey`)
2. `Tars:Data:TenantConnectionTemplates:{databaseKey}:Template` — template expanded with placeholders (only when there is a `TenantKey`)
3. `Tars:Data:Connections:{databaseKey}` — static/shared connection

Practical consequences:

- with a current tenant + an entry in `TenantConnections`, the tenant entry wins;
- with no specific entry but with a template, the template is expanded;
- with neither, it falls back to the static `Connections` connection.

Supported template placeholders: `{tenantKey}` and `{tenantCode}`.

The `CompositeDataConnectionResolver` chains all registered `IDataConnectionResolver`s (including custom resolvers added via `TryAddEnumerable`) and returns the **first non-null result**.

---

## 5. Configuration patterns (appsettings)

### 5.1 Explicit keys only (recommended for multi-database)

```json
{
  "Tars": {
    "Data": {
      "Connections": {
        "central": {
          "ConnectionString": "Host=localhost;Port=5433;Database=central;Username=postgres;Password=postgres",
          "Provider": "PostgreSQL"
        },
        "shared": {
          "ConnectionString": "Host=localhost;Port=5433;Database=shared;Username=postgres;Password=postgres",
          "Provider": "PostgreSQL"
        }
      },
      "TenantConnections": {
        "primary": {
          "dev": {
            "ConnectionString": "Host=localhost;Port=5433;Database=dev_primary;Username=postgres;Password=postgres",
            "Provider": "PostgreSQL"
          }
        },
        "secondary": {
          "dev": {
            "ConnectionString": "Host=localhost;Port=5433;Database=dev_secondary;Username=postgres;Password=postgres",
            "Provider": "PostgreSQL"
          }
        }
      }
    }
  }
}
```

Code with explicit intent:

```csharp
_uowFactory.Create("central");
_uowFactory.Create("primary");   // resolves to the current tenant's database
_uowFactory.Create("secondary");
```

### 5.2 Contextual `default`

Some apps prefer `default` as the "current operational database" and `central` when global access must be forced:

```json
{
  "Tars": {
    "Data": {
      "Connections": {
        "default":  { "ConnectionString": "Host=localhost;Database=central;Username=postgres;Password=postgres", "Provider": "PostgreSQL" },
        "central":  { "ConnectionString": "Host=localhost;Database=central;Username=postgres;Password=postgres", "Provider": "PostgreSQL" }
      },
      "TenantConnections": {
        "default": {
          "dev": { "ConnectionString": "Host=localhost;Database=dev_primary;Username=postgres;Password=postgres", "Provider": "PostgreSQL" }
        }
      }
    }
  }
}
```

Behavior:

- with no tenant: `default` resolves to `central` (static entry);
- with tenant `dev`: `default` resolves to the tenant's primary (entry in `TenantConnections:default:dev`);
- `central` always resolves to central.

Useful, but less explicit than always using named roles.

### 5.3 Tenant templates

When the naming convention is stable:

```json
{
  "Tars": {
    "Data": {
      "TenantConnectionTemplates": {
        "primary": {
          "Template": "Host=localhost;Port=5433;Database={tenantKey}_primary;Username=postgres;Password=postgres",
          "Provider": "PostgreSQL"
        },
        "secondary": {
          "Template": "Host=localhost;Port=5433;Database={tenantKey}_secondary;Username=postgres;Password=postgres",
          "Provider": "PostgreSQL"
        }
      }
    }
  }
}
```

---

## 6. Configuration and DI must match

Having the key in configuration **is not enough**. If the code uses `_uowFactory.Create("central")`, then **two** registrations must exist:

1. configuration for `central` (in `Connections`, `TenantConnections` or `TenantConnectionTemplates`);
2. a pipeline registered for `central` via `AddTarsData<T>("central", ...)`.

If the configuration exists but the pipeline was not registered, creating the context/unit of work for that key fails at runtime.

### Infrastructure registration (once)

Each method registers **a single service**, allowing individual components to be replaced:

```csharp
services.AddTarsDataContextAccessor();
services.AddTarsRelationalCompositeConnectionResolver();
services.AddTarsRelationalConfigurationConnectionResolver();
services.AddTarsDataContextFactory();
services.AddTarsRelationalUnitOfWorkFactory();

// one AddTarsData<T> per database (see sections below)

// repositories
services.AddTarsDataRepositoriesFromAssemblies(typeof(AppAssemblyMarker).Assembly);
```

---

## 7. Tenant resolution

Tenant resolution is pluggable and composed of resolvers in order. The framework does not assume that the tenant identity always comes from a subdomain, header, claim or HTTP.

```csharp
builder.Services.AddTarsMultitenancy();
builder.Services.AddTarsHeaderTenantResolver("X-Tenant-Key");
builder.Services.AddTarsTenantResolution(options =>
{
    // the first resolver that returns a tenant wins
    options.AddResolver(new ClaimTenantResolver("tenant_key"));
    options.AddResolver<HeaderTenantResolver>();
});

var app = builder.Build();
app.UseTarsTenantResolution();   // middleware (Multitenancy.AspNetCore)
```

HTTP example:

```http
GET /api/v1/companies
X-Tenant-Key: dev
```

With the tenant `dev` resolved:

- `primary` resolves to the `dev` primary connection;
- `secondary` resolves to the `dev` secondary connection;
- `central` keeps resolving to the central connection.

### Execution outside HTTP

Jobs and hosted services have no `HttpContext`. The tenant is established via `ITenantExecutionRunner` (see [per-tenant execution](../multitenancy/execution.md)), which sets the ambient `ITenantContext` before running the work — so `_uowFactory.Create("primary")` works the same inside the scope.

> Complete reference of the multitenancy DI methods: [Multitenancy — Configuration](../multitenancy/configuration.md).

---

## 8. Access patterns: factory, context and unit of work

### 8.1 With `IUnitOfWorkFactory`

Delegate (auto-commit + auto-dispose):

```csharp
var total = await _uowFactory.ExecuteAsync("central", async (context, ct) =>
{
    var repository = context.AcquireRepository<ICompanyRepository>();
    return await repository.CountAsync(ct);
}, cancellationToken: cancellationToken);
```

Manual (conditional commit control):

```csharp
await using var uow = _uowFactory.Create("central");
var context = await uow.GetContextAsync(cancellationToken);
var repository = context.AcquireRepository<ICompanyRepository>();
// ... logic ...
await uow.CommitAsync(cancellationToken);
```

The keyless overload uses `DataKeys.Default`:

```csharp
await using var uow = _uowFactory.Create();          // "default"
await _uowFactory.ExecuteAsync(async (ctx, ct) => { /* ... */ });
```

### 8.2 With `IDataContextFactory`

```csharp
// reuses the ambient context for the same key (same connection/transaction)
await using var ctx = await _dataContextFactory.CreateScopedAsync("central", ct);
var repo = ctx.AcquireRepository<ICompanyRepository>();

// context independent from the ambient scope (jobs, auditing, parallelism)
await using var isolated = await _dataContextFactory.CreateIsolatedAsync("secondary", ct);
```

| Method | Use when |
|---|---|
| `CreateScopedAsync` | nested code should share the same connection/transaction |
| `CreateIsolatedAsync` | jobs, audit writes, out-of-band access, parallel operations |

### 8.3 Keyed ambient context

Unlike stacks with a single global `Current`, the `Tars` ambient context is **keyed by database role**. This allows keeping, within the same flow, one context for `central` and another for `primary`:

```csharp
await using var catalog    = await _dataContextFactory.CreateScopedAsync("central", ct);
await using var tenantMain = await _dataContextFactory.CreateScopedAsync("primary", ct);

var companies = catalog.AcquireRepository<ICompanyCatalogRepository>();
var users     = tenantMain.AcquireRepository<IUserRepository>();
```

---

## 9. Complete combinations by scenario

### 9.1 Single database

```csharp
services.AddTarsDataContextAccessor();
services.AddTarsRelationalCompositeConnectionResolver();
services.AddTarsRelationalConfigurationConnectionResolver();
services.AddTarsDataContextFactory();
services.AddTarsRelationalUnitOfWorkFactory();

services.AddTarsData<AppDbContext>((sp, d) =>
    new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(d.ConnectionString).Options);
```

```json
{ "Tars": { "Data": { "Connections": {
  "default": { "ConnectionString": "Host=localhost;Database=app;Username=postgres;Password=postgres", "Provider": "PostgreSQL" }
} } } }
```

```csharp
await _uowFactory.ExecuteAsync(async (ctx, ct) =>
{
    var repo = ctx.AcquireRepository<IOrderRepository>();
    await repo.AddAsync(order, ct);
});
```

### 9.2 One shared database + per-tenant databases

```csharp
// infrastructure (same as 9.1) +
services.AddTarsData<CentralDbContext>("central", (sp, d) =>
    new DbContextOptionsBuilder<CentralDbContext>().UseNpgsql(d.ConnectionString).Options);
services.AddTarsData<TenantDbContext>("primary", (sp, d) =>
    new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(d.ConnectionString).Options);
services.AddTarsData<TenantAnalyticsDbContext>("secondary", (sp, d) =>
    new DbContextOptionsBuilder<TenantAnalyticsDbContext>().UseNpgsql(d.ConnectionString).Options);
```

Configuration: `central` in `Connections`; `primary`/`secondary` in `TenantConnections` (or templates). Usage:

```csharp
await using var catalog    = _uowFactory.Create("central");
await using var tenantMain = _uowFactory.Create("primary");
```

### 9.3 Database-per-tenant via `TenantConnections`

Same DI registration as 9.2 for the tenant key; all the variation lives in configuration:

```json
{ "Tars": { "Data": { "TenantConnections": { "primary": {
  "acme":   { "ConnectionString": "Host=acme-db;Database=acme;Username=app;Password=x",   "Provider": "PostgreSQL" },
  "globex": { "ConnectionString": "Host=globex-db;Database=globex;Username=app;Password=y","Provider": "PostgreSQL" }
} } } } }
```

With the tenant resolved, `_uowFactory.Create("primary")` points to the correct database with no code change.

### 9.4 Database-per-tenant via template

Replaces the per-tenant block with a template (see §5.3). Ideal when databases follow `{tenantKey}_primary`. `_uowFactory.Create("primary")` stays the same.

### 9.5 HTTP with a tenant header

```csharp
builder.Services.AddTarsMultitenancy();
builder.Services.AddTarsHeaderTenantResolver("X-Tenant-Key");
builder.Services.AddTarsTenantResolution(o => o.AddResolver<HeaderTenantResolver>());
// + data infrastructure + AddTarsData<T>("primary", ...)

var app = builder.Build();
app.UseTarsTenantResolution();
```

Handlers only need `_uowFactory.Create("primary")`; the resolver maps it to the current tenant.

### 9.6 Worker iterating over all tenants

```csharp
var runner  = sp.GetRequiredService<ITenantExecutionRunner>();
var catalog = sp.GetRequiredService<ITenantCatalog>();

await runner.RunForEachTenantAsync(
    catalog.ListAsync(ct),
    async (services, tenantCtx, innerCt) =>
    {
        var uowFactory = services.GetRequiredService<IUnitOfWorkFactory>();
        await uowFactory.ExecuteAsync("primary", async (context, c) =>
        {
            var repo = context.AcquireRepository<IUserRepository>();
            // per-tenant work
        }, cancellationToken: innerCt);
    },
    new TenantExecutionOptions { MaxDegreeOfParallelism = 3 },
    ct);
```

### 9.7 Schema-per-tenant and row-level

These strategies live inside the `DbContext` (not in connection resolution). See complete examples of `ITenantSchemaProvider`, `HasDefaultSchema` and `HasQueryFilter` in [Multitenancy — Data isolation](../multitenancy/data-isolation.md), including the combination of database-per-tenant for enterprise + schema-per-tenant for standard.

---

## 10. Multi-database coordination and transactions

Design rule: `Tars` supports cross-database orchestration **without** pretending that distributed transactions are always easy or safe.

| Level | Strategy | Guarantee |
|---|---|---|
| 0 | isolated local commits (each UoW commits on its own) | simplest and most common; no atomicity across databases |
| 1 | best-effort sequential coordination (`IMultiDatabaseCoordinator`) | tries to commit in order; optional compensation; **not** atomic |
| 2 | distributed transactions | only in restricted scenarios, always opt-in; never the default |
| 3 | outbox / eventual consistency | recommended for serious cross-database workflows |

### Level 1 — `IMultiDatabaseCoordinator`

```csharp
services.AddTarsMultiDatabaseCoordination();
```

```csharp
await _coordinator.ExecuteAsync(
    databaseKeys: ["central", "primary"],
    work: async (mdb, ct) =>
    {
        var centralUow = mdb.GetUnitOfWork("central");
        var tenantUow  = mdb.GetUnitOfWork("primary");

        var centralCtx = await centralUow.GetContextAsync(ct);
        var tenantCtx  = await tenantUow.GetContextAsync(ct);

        centralCtx.AcquireRepository<IAuditRepository>().Add(auditEntry);
        tenantCtx.AcquireRepository<IOrderRepository>().Add(order);
        // the coordinator commits each UoW in order at the end
    },
    compensate: async (mdb, ex, ct) =>
    {
        // called on partial failure — idempotency is the caller's responsibility
    },
    cancellationToken: ct);
```

> The coordinator is best-effort sequential (Level 1). On failure after a partial commit, `compensate` is invoked if provided — but idempotency is your responsibility. For strong guarantees, prefer an outbox pattern (Level 3).

Recommendation: local commits by default; best-effort coordination as opt-in; an outbox-friendly architecture for advanced scenarios.

---

## 11. Design rules

- Use logical keys in code, never physical database names.
- Prefer explicit keys (`central`/`primary`/`secondary`) for multi-database apps.
- Keep `default` only when the team wants contextual convenience on purpose.
- Treat tenant resolution and data resolution as separate concerns.
- Register an `AddTarsData<T>` for each key the code uses.
- Ensure configuration and DI agree on the same keys.
- Use templates only when the tenant naming convention is stable.
- Do not assume HTTP is the only execution environment; use `ITenantExecutionRunner` in jobs.
- Treat cross-database writes as coordinated workflows, not automatic atomic transactions.

---

## 12. Common mistakes

**Key exists in config but not in DI** — `_uowFactory.Create("central")` fails. *Fix:* register `AddTarsData<T>("central", ...)`.

**Using physical names as the runtime key** — the code becomes tied to the environment. *Fix:* use logical roles.

**Expecting tenant keys without a tenant context** — `primary` does not resolve in jobs or before resolution. *Fix:* establish the tenant via `ITenantExecutionRunner`, or use a shared key such as `central`.

**Using `default` everywhere in a multi-role architecture** — hidden routing bugs between central and tenant. *Fix:* use `default` only for truly contextual flows; explicit keys for central/shared.

---

## 13. Practical recommendation

For most serious applications, the best starting point is the set of roles:

- `central` — shared central database
- `shared` — shared auxiliary database
- `primary` — the current tenant's main database
- `secondary` — the current tenant's secondary database

Keep `default` only if the team really wants a shortcut. This gives clearer code, easier debugging, less ambiguity when there is a tenant context, and a clean growth path toward more databases in the future.
