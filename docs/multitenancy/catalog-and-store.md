# Tenant Catalog and Store

## Difference between catalog and store

| | `ITenantCatalog` | `ITenantStore` |
|---|---|---|
| **Purpose** | Enumeration of all tenants | Point lookup by ID or name |
| **Return** | `IAsyncEnumerable<ITenantContext>` | `Task<ITenantContext?>` |
| **Use cases** | Workers, jobs, synchronization, reports | Tenant resolution from external data, validation |
| **Recommended lifetime** | Singleton | Singleton or Scoped |

Both can coexist. A system may need to iterate over all tenants (catalog) and also perform a point lookup (store).

---

## `ITenantCatalog`

### `InMemoryTenantCatalog` (built-in)

Catalog with a fixed list of tenants. Useful for dev, tests and fixed-tenant deployments.

```csharp
// By list of keys
builder.Services.AddTarsInMemoryTenantCatalog(["acme", "globex", "initech"]);

// By full contexts (with a code different from the key)
builder.Services.AddTarsInMemoryTenantCatalog(
    new[]
    {
        TenantContext.Create("acme", tenantCode: "ACME"),
        TenantContext.Create("globex", tenantCode: "GLOBEX"),
    });
```

### Custom catalog

For production, implement `ITenantCatalog` reading from a database, API or configuration:

```csharp
public sealed class DatabaseTenantCatalog : ITenantCatalog
{
    private readonly IDbConnectionFactory _db;

    public DatabaseTenantCatalog(IDbConnectionFactory db) => _db = db;

    public async IAsyncEnumerable<ITenantContext> ListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenAsync(cancellationToken);
        var tenants = await conn.QueryAsync<TenantRecord>("SELECT key, code FROM tenants WHERE active = 1");

        foreach (var t in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return TenantContext.Create(t.Key, tenantCode: t.Code);
        }
    }
}
```

Registration:

```csharp
builder.Services.AddTarsTenantCatalog<DatabaseTenantCatalog>();
```

### Catalog loaded from `appsettings`

A common pattern for environments with a fixed number of configurable tenants:

```json
// appsettings.json
{
  "Tenants": {
    "Known": ["acme", "globex", "initech"]
  }
}
```

```csharp
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

Registration:

```csharp
builder.Services.AddTarsTenantCatalog<ConfigurationTenantCatalog>();
```

---

## `ITenantStore`

### Custom implementation

The framework does not provide a built-in `ITenantStore` implementation — it depends on the consumer's data source (relational database, Redis, external API, etc.).

```csharp
public sealed class DatabaseTenantStore : ITenantStore
{
    private readonly IDbConnectionFactory _db;

    public DatabaseTenantStore(IDbConnectionFactory db) => _db = db;

    public async Task<ITenantContext?> FindByIdAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenAsync(cancellationToken);
        var record = await conn.QuerySingleOrDefaultAsync<TenantRecord>(
            "SELECT key, code FROM tenants WHERE key = @key AND active = 1",
            new { key = tenantId });

        return record is null ? null : TenantContext.Create(record.Key, tenantCode: record.Code);
    }

    public async Task<ITenantContext?> FindByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenAsync(cancellationToken);
        var record = await conn.QuerySingleOrDefaultAsync<TenantRecord>(
            "SELECT key, code FROM tenants WHERE display_name = @name AND active = 1",
            new { name });

        return record is null ? null : TenantContext.Create(record.Key, tenantCode: record.Code);
    }
}
```

Registration:

```csharp
builder.Services.AddTarsTenantStore<DatabaseTenantStore>();
```

### Store with in-memory cache

To avoid round-trips to the database on every resolution:

```csharp
public sealed class CachedTenantStore : ITenantStore
{
    private readonly ITenantStore _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);

    public CachedTenantStore(ITenantStore inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<ITenantContext?> FindByIdAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant:id:{tenantId}";
        if (_cache.TryGetValue(cacheKey, out ITenantContext? cached))
            return cached;

        var result = await _inner.FindByIdAsync(tenantId, cancellationToken);
        if (result is not null)
            _cache.Set(cacheKey, result, _ttl);

        return result;
    }

    public Task<ITenantContext?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
        => _inner.FindByNameAsync(name, cancellationToken);
}
```

### Using the store in the resolution pipeline

A common case is combining the store with the resolver to validate that the tenant exists and is active before accepting the request:

```csharp
public sealed class ValidatingHeaderTenantResolver : ITenantResolver
{
    private readonly ITenantStore _store;

    public ValidatingHeaderTenantResolver(ITenantStore store) => _store = store;

    public async ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Items.TryGetValue(TenantResolutionHttpKeys.HttpRequestData, out var obj) ||
            obj is not TenantHttpRequestData data)
            return TenantResolutionResult.Unresolved();

        if (!data.Headers.TryGetValue("X-Tenant-Key", out var headerValue) ||
            string.IsNullOrWhiteSpace(headerValue))
            return TenantResolutionResult.Unresolved();

        var tenant = await _store.FindByIdAsync(headerValue, cancellationToken);
        if (tenant is null)
            return TenantResolutionResult.Unresolved();

        return TenantResolutionResult.Resolved(tenant.TenantKey!, tenantCode: tenant.TenantCode);
    }
}
```

Registration:

```csharp
builder.Services.AddTarsTenantStore<DatabaseTenantStore>();
builder.Services.AddSingleton<ValidatingHeaderTenantResolver>();
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<ValidatingHeaderTenantResolver>();
});
```
