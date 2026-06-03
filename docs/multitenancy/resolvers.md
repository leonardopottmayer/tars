# Tenant Resolvers

## How resolution works

The framework tries the registered resolvers in order, one by one. The first one that returns `IsResolved = true` ends the chain. If no resolver identifies the tenant, the context ends up with `IsResolved = false`.

The registration order in `AddTarsTenantResolution` is the attempt order.

---

## Built-in resolvers

### `HeaderTenantResolver` *(requires `Multitenancy.AspNetCore`)*

Resolves the tenant from an HTTP header. The default header is `X-Tenant-Key`.

```csharp
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver(new HeaderTenantResolver("X-Tenant-Key"));
});
```

Or via the helper:

```csharp
builder.Services.AddTarsHeaderTenantResolver("X-Tenant-Key");
// You still need to register the resolver in the pipeline:
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<HeaderTenantResolver>();
});
```

Usage in the call:

```http
GET /api/products
X-Tenant-Key: acme
```

---

### `SubdomainTenantResolver` *(requires `Multitenancy.AspNetCore`)*

Resolves the tenant from the first segment of the subdomain. E.g. `acme.app.com` → tenant key `acme`.

It only works when the host has three or more segments (e.g. `acme.app.com`). On hosts without a subdomain (`app.com`) it returns unresolved.

```csharp
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<SubdomainTenantResolver>();
});
builder.Services.AddTarsSubdomainTenantResolver();
```

---

### `ClaimTenantResolver` *(`Multitenancy` package, no HTTP dependency)*

Resolves the tenant from a claim on the `ClaimsPrincipal`. Useful when the JWT already carries the tenant (SaaS with per-user login), for tenant impersonation by admins, and in workers that receive contextual tokens.

```csharp
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver(new ClaimTenantResolver("tenant_key"));
});
```

The default claim is `"tenant_key"`. In ASP.NET Core, the tenant middleware must come **after** `UseAuthentication` so that `ClaimsPrincipal` is already populated.

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseTarsTenantResolution(); // ClaimsPrincipal already available here
```

---

### `StaticTenantResolver` *(`Multitenancy` package)*

Always returns the same tenant key. Useful for single-tenant deployments, CLI tools, local dev and tests.

```csharp
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver(new StaticTenantResolver("acme"));
});
```

---

### `NullTenantResolver` *(`Multitenancy` package)*

Always returns unresolved. Useful as a sentinel or an explicit no-op in compositions.

---

## Combining resolvers

Resolvers are tried in registration order. Example of a real pipeline with fallback:

```csharp
builder.Services.AddTarsTenantResolution(options =>
{
    // 1st try the JWT claim (logged-in user)
    options.AddResolver(new ClaimTenantResolver("tenant_key"));
    // 2nd try the header (service-to-service / gateway calls)
    options.AddResolver(new HeaderTenantResolver("X-Tenant-Key"));
    // 3rd try the subdomain (public app)
    options.AddResolver<SubdomainTenantResolver>();
});
```

---

## Creating a custom resolver

Implement `ITenantResolver` and use `TenantResolutionRequest` to access the available context.

```csharp
public sealed class RouteValueTenantResolver : ITenantResolver
{
    // Key used by the middleware to deposit the HttpContext into Items
    private const string RouteDataKey = "HttpRouteValues";

    public ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Items.TryGetValue(RouteDataKey, out var obj) ||
            obj is not IDictionary<string, object?> routeValues)
            return ValueTask.FromResult(TenantResolutionResult.Unresolved());

        if (!routeValues.TryGetValue("tenantKey", out var value) || value is not string tenantKey)
            return ValueTask.FromResult(TenantResolutionResult.Unresolved());

        return ValueTask.FromResult(TenantResolutionResult.Resolved(tenantKey));
    }
}
```

For the route data to be available in `Items`, the ASP.NET Core middleware must deposit it before calling the pipeline. See the pattern followed by `TarsTenantResolutionMiddleware` as a reference.

Registration:

```csharp
builder.Services.AddSingleton<RouteValueTenantResolver>();
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<RouteValueTenantResolver>();
});
```

---

## Resolver with extra data via `Metadata`

`TenantResolutionResult.Metadata` allows passing additional data from the resolver to the consumer without changing the contract. Useful when the resolver already carries data the rest of the application needs.

```csharp
public sealed class DatabaseTenantResolver : ITenantResolver
{
    private readonly ITenantRepository _repo;

    public DatabaseTenantResolver(ITenantRepository repo) => _repo = repo;

    public async ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var headerKey = /* read from Items */;
        if (headerKey is null) return TenantResolutionResult.Unresolved();

        var tenant = await _repo.FindByKeyAsync(headerKey, cancellationToken);
        if (tenant is null) return TenantResolutionResult.Unresolved();

        return TenantResolutionResult.Resolved(
            tenantKey: tenant.Key,
            tenantCode: tenant.Code,
            metadata: tenant); // full entity available as Metadata
    }
}
```

`Metadata` is available in `TenantResolutionResult` during the creation of the `TenantContext`, but it is not exposed by default on `ITenantContext`. To access it, cast to `TenantContext` (the concrete implementation) or populate `Properties` in a custom factory.

---

## `TenantResolutionResult`

| Member | Description |
|---|---|
| `IsResolved` | `true` when the resolver identified the tenant |
| `TenantKey` | Technical identifier (e.g. `"acme"`) |
| `TenantCode` | Alternative code; equal to `TenantKey` when not provided |
| `Metadata` | Free-form extra data from the resolver (e.g. the full tenant entity) |

Static factories:

```csharp
TenantResolutionResult.Unresolved();
TenantResolutionResult.Resolved("acme");
TenantResolutionResult.Resolved("acme", tenantCode: "ACME", metadata: tenantEntity);
```
