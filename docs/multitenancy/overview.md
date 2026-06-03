# Multitenancy — Overview

## Packages

| Package | Level | Role |
|---|---|---|
| `Pottmayer.Tars.Multitenancy.Abstractions` | Abstractions | Contracts: `ITenantContext`, `ITenantContextAccessor`, `ITenantResolver`, `ITenantResolverPipeline`, `ITenantCatalog`, `ITenantStore`, execution |
| `Pottmayer.Tars.Multitenancy` | Runtime | Resolution pipeline, agnostic resolvers, in-memory catalog, per-tenant execution |
| `Pottmayer.Tars.Multitenancy.AspNetCore` | Host Integration | Middleware, HTTP resolvers (`header`, `subdomain`) |

---

## Core principle

The module works in any host — Web API, Worker Service, Console, tests. The `AspNetCore` package only adds the HTTP-specific pieces. All the context, resolution and execution logic lives in the agnostic runtime.

---

## What the module provides

- **Ambient tenant context**: the current tenant is available at any point of the execution via `ITenantContext` / `ITenantContextAccessor`
- **Resolution pipeline**: multiple resolvers in priority order (header, subdomain, claim, static, custom)
- **Catalog**: enumerates all known tenants (`ITenantCatalog`)
- **Store**: point lookups by ID or name (`ITenantStore`)
- **Per-tenant execution**: creates an isolated DI scope per tenant for workers and jobs (`ITenantExecutionRunner`)

---

## Main contracts

### `ITenantContext`

Represents the tenant resolved for the current execution.

```csharp
public interface ITenantContext
{
    bool IsResolved { get; }
    string? TenantKey { get; }   // technical identifier (e.g. "acme")
    string? TenantCode { get; }  // alternative code; equal to TenantKey when not specified
    IReadOnlyDictionary<string, object?> Properties { get; }
}
```

`Properties` is the free-form dictionary for extra tenant data without creating fields in the base contract.

### `ITenantContextAccessor`

Holds the ambient context via `AsyncLocal<T>` — it flows correctly across `await` within the same execution context.

```csharp
public interface ITenantContextAccessor
{
    ITenantContext? Current { get; }
    void SetCurrent(ITenantContext? context);
}
```

### `ITenantResolver`

Implemented by any resolver that tries to identify the current tenant.

```csharp
public interface ITenantResolver
{
    ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default);
}
```

`TenantResolutionRequest` is the agnostic context vector passed to each resolver:

```csharp
public sealed class TenantResolutionRequest
{
    public IServiceProvider Services { get; init; }
    public string? ExplicitTenantKey { get; init; }
    public ClaimsPrincipal? Principal { get; init; }
    public IReadOnlyDictionary<string, object?> Items { get; init; }
}
```

The ASP.NET Core middleware populates `Items` with data from the `HttpContext` (host, headers) so that resolvers do not depend on `IHttpContextAccessor`.

### `ITenantCatalog`

Enumerates all known tenants. Used in workers and jobs that need to iterate.

```csharp
public interface ITenantCatalog
{
    IAsyncEnumerable<ITenantContext> ListAsync(CancellationToken cancellationToken = default);
}
```

### `ITenantStore`

Point lookup by ID or name. Complements the catalog for scenarios where a specific tenant must be found.

```csharp
public interface ITenantStore
{
    Task<ITenantContext?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<ITenantContext?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
}
```

---

## Minimal registration

```csharp
// Program.cs
builder.Services.AddTarsMultitenancy();
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver(new HeaderTenantResolver("X-Tenant-Key"));
});
```

For complete scenarios with all options, see [configuration.md](./configuration.md).

---

## Topics

- [Resolvers](./resolvers.md): all built-in resolvers and how to create a custom one
- [Catalog and Store](./catalog-and-store.md): `ITenantCatalog`, `ITenantStore`, implementations
- [Per-tenant execution](./execution.md): `ITenantExecutionRunner`, `ITenantExecutionScopeFactory`, workers and jobs
- [ASP.NET Core](./aspnetcore.md): middleware, HTTP resolvers, order in the pipeline
- [Data isolation](./data-isolation.md): `ITenantConnectionStringProvider`, `ITenantSchemaProvider`
- [Configuration](./configuration.md): appsettings, full DI, all scenarios
