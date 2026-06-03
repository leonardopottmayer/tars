# Per-Tenant Data Isolation

## Package

`Pottmayer.Tars.Data.Relational.Abstractions`  
Namespace: `Pottmayer.Tars.Data.Relational.Abstractions.Multitenancy`

---

## Isolation strategies

| Strategy | Interface | When to use |
|---|---|---|
| Database per tenant | `ITenantConnectionStringProvider` | Strong isolation; each tenant has its own database |
| Schema per tenant | `ITenantSchemaProvider` | Shared database, isolated by schema |
| Row-level (TenantKey column) | None — filter in the repository | Shared database and schema |

The interfaces are not mutually exclusive. A system can use database-per-tenant for enterprise customers and row-level for standard customers, for example.

---

## `ITenantConnectionStringProvider`

```csharp
public interface ITenantConnectionStringProvider
{
    Task<string> GetConnectionStringAsync(string tenantId, CancellationToken cancellationToken = default);
}
```

### `appsettings`-based implementation

```json
// appsettings.json
{
  "Tenants": {
    "ConnectionStrings": {
      "acme": "Server=acme-db;Database=AcmeDb;User Id=app;Password=xxx;",
      "globex": "Server=shared-db;Database=GlobexDb;User Id=app;Password=yyy;"
    }
  }
}
```

```csharp
public sealed class ConfigurationTenantConnectionStringProvider : ITenantConnectionStringProvider
{
    private readonly IConfiguration _configuration;

    public ConfigurationTenantConnectionStringProvider(IConfiguration configuration)
        => _configuration = configuration;

    public Task<string> GetConnectionStringAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var cs = _configuration[$"Tenants:ConnectionStrings:{tenantId}"];
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException($"No connection string configured for tenant '{tenantId}'.");

        return Task.FromResult(cs);
    }
}
```

Registration:

```csharp
builder.Services.AddSingleton<ITenantConnectionStringProvider, ConfigurationTenantConnectionStringProvider>();
```

### Central-database-based implementation

When the connection strings live in a central metadata database:

```csharp
public sealed class DatabaseTenantConnectionStringProvider : ITenantConnectionStringProvider
{
    private readonly ITenantMetadataRepository _repo;

    public DatabaseTenantConnectionStringProvider(ITenantMetadataRepository repo)
        => _repo = repo;

    public async Task<string> GetConnectionStringAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var metadata = await _repo.GetAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant '{tenantId}' not found.");

        return metadata.ConnectionString;
    }
}
```

### Using the provider in the `DbContext`

Integrate with EF Core via `IDbContextFactory` or a custom `OnConfiguring`:

```csharp
public sealed class TenantDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly ITenantConnectionStringProvider _connectionStrings;
    private readonly ITenantContextAccessor _accessor;

    public TenantDbContextFactory(
        ITenantConnectionStringProvider connectionStrings,
        ITenantContextAccessor accessor)
    {
        _connectionStrings = connectionStrings;
        _accessor = accessor;
    }

    public async Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var tenantKey = _accessor.Current?.TenantKey
            ?? throw new InvalidOperationException("Tenant context not set.");

        var connectionString = await _connectionStrings.GetConnectionStringAsync(tenantKey, cancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
```

---

## `ITenantSchemaProvider`

```csharp
public interface ITenantSchemaProvider
{
    string GetSchema(string tenantId);
}
```

### Default implementation — schema = tenantId

```csharp
public sealed class TenantKeySchemaProvider : ITenantSchemaProvider
{
    public string GetSchema(string tenantId) => tenantId.ToLowerInvariant();
}
```

### Implementation with a prefix

```csharp
public sealed class PrefixedTenantSchemaProvider : ITenantSchemaProvider
{
    private readonly string _prefix;

    public PrefixedTenantSchemaProvider(string prefix = "tenant_")
        => _prefix = prefix;

    public string GetSchema(string tenantId) => $"{_prefix}{tenantId.ToLowerInvariant()}";
}
```

Result: tenant `acme` → schema `tenant_acme`.

### Using the provider in EF Core

```csharp
public class AppDbContext : DbContext
{
    private readonly ITenantSchemaProvider _schemaProvider;
    private readonly ITenantContextAccessor _accessor;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantSchemaProvider schemaProvider,
        ITenantContextAccessor accessor) : base(options)
    {
        _schemaProvider = schemaProvider;
        _accessor = accessor;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var tenantKey = _accessor.Current?.TenantKey ?? "public";
        var schema = _schemaProvider.GetSchema(tenantKey);

        modelBuilder.HasDefaultSchema(schema);

        // All entities will use the tenant's schema
        base.OnModelCreating(modelBuilder);
    }
}
```

---

## Row-level isolation (no specific interface)

For row-level, the pattern is to apply a global filter in the `DbContext`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    var tenantKey = _accessor.Current?.TenantKey;

    modelBuilder.Entity<Order>()
        .HasQueryFilter(o => o.TenantKey == tenantKey);

    modelBuilder.Entity<Product>()
        .HasQueryFilter(p => p.TenantKey == tenantKey);
}
```

> **Caution**: if the `DbContext` is registered as `Scoped` (the default), the filter is rebuilt on each request. If it is registered as `Singleton`, the filter is fixed at the first construction — do not use row-level with a singleton `DbContext`.

---

## Combining strategies

Scenario: enterprise customers have their own database, standard customers share a database with a schema per tenant.

```csharp
public sealed class HybridTenantConnectionStringProvider : ITenantConnectionStringProvider
{
    private readonly IConfiguration _config;

    public HybridTenantConnectionStringProvider(IConfiguration config) => _config = config;

    public Task<string> GetConnectionStringAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        // Enterprise: its own connection string
        var enterpriseCs = _config[$"Tenants:Enterprise:{tenantId}:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(enterpriseCs))
            return Task.FromResult(enterpriseCs);

        // Standard: shared connection string
        var sharedCs = _config["Tenants:Shared:ConnectionString"]
            ?? throw new InvalidOperationException("Shared connection string not configured.");

        return Task.FromResult(sharedCs);
    }
}

public sealed class HybridTenantSchemaProvider : ITenantSchemaProvider
{
    private readonly IConfiguration _config;

    public HybridTenantSchemaProvider(IConfiguration config) => _config = config;

    public string GetSchema(string tenantId)
    {
        // Enterprise have their own database — default schema
        var isEnterprise = _config[$"Tenants:Enterprise:{tenantId}:ConnectionString"] is not null;
        return isEnterprise ? "dbo" : $"tenant_{tenantId}";
    }
}
```

```json
// appsettings.json
{
  "Tenants": {
    "Shared": {
      "ConnectionString": "Server=shared-db;Database=MultiTenantDb;..."
    },
    "Enterprise": {
      "acme": {
        "ConnectionString": "Server=acme-exclusive;Database=AcmeDb;..."
      }
    }
  }
}
```
