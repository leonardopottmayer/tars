# Data — Configuration (Relational Axis)

> This page covers the configuration of the **relational** axis (EF Core + Dapper).

## appsettings structure

### Single-database app

```json
{
  "Tars": {
    "Data": {
      "Connections": {
        "default": {
          "ConnectionString": "Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=postgres",
          "Provider": "PostgreSQL"
        }
      }
    }
  }
}
```

### Multi-database app (two databases)

```json
{
  "Tars": {
    "Data": {
      "Connections": {
        "default": {
          "ConnectionString": "Host=localhost;Database=tenant_app;Username=app;Password=secret",
          "Provider": "PostgreSQL"
        },
        "central": {
          "ConnectionString": "Host=central-db.internal;Database=central;Username=app;Password=secret",
          "Provider": "PostgreSQL"
        }
      }
    }
  }
}
```

### Multitenant app — per-tenant connection

```json
{
  "Tars": {
    "Data": {
      "Connections": {
        "default": {
          "ConnectionString": "Host=shared.internal;Database=shared;Username=app;Password=secret",
          "Provider": "PostgreSQL"
        }
      },
      "TenantConnections": {
        "default": {
          "tenant-a": {
            "ConnectionString": "Host=tenant-a.internal;Database=app_a;Username=app;Password=secret",
            "Provider": "PostgreSQL"
          },
          "tenant-b": {
            "ConnectionString": "Host=tenant-b.internal;Database=app_b;Username=app;Password=secret",
            "Provider": "PostgreSQL"
          }
        }
      }
    }
  }
}
```

> When `TenantConnections` is present for the database key and the current tenant,
> the `ConfigurationDataConnectionResolver` uses the tenant's connection string.
> `IDataConnectionDescriptor.IsTenantScoped` will be `true`.

### Multitenant app — connection template

Useful when the URL follows a predictable pattern:

```json
{
  "Tars": {
    "Data": {
      "TenantConnectionTemplates": {
        "default": {
          "Template": "Host=db.internal;Database=app_{tenantCode};Username=app;Password=secret;",
          "Provider": "PostgreSQL"
        }
      }
    }
  }
}
```

Available placeholders: `{tenantKey}`, `{tenantCode}`.

---

## Supported providers (`DbProvider`)

| Value | Usage |
|---|---|
| `PostgreSQL` | Npgsql |
| `SqlServer` | Microsoft.Data.SqlClient / SqlServer EF |
| `MySql` | Pomelo / MySqlConnector |
| `Sqlite` | Sqlite EF |
| `Oracle` | Oracle EF |
| `Unknown` | Fallback; buildOptions receives the descriptor with no provider defined |

---

## DI — registration principle

Each DI method registers **a single service**. This allows the application to replace any individual component with its own implementation without rewriting the rest of the stack.

### Single-database (key `"default"`)

```csharp
// Infrastructure — register each component separately
services.AddTarsDataContextAccessor();
services.AddTarsRelationalCompositeConnectionResolver();
services.AddTarsRelationalConfigurationConnectionResolver();
services.AddTarsDataContextFactory();
services.AddTarsRelationalUnitOfWorkFactory();

// Database pipeline (no explicit key — uses "default")
services.AddTarsData<AppDbContext>((sp, descriptor) =>
{
    var options = new DbContextOptionsBuilder<AppDbContext>();
    options.UseNpgsql(descriptor.ConnectionString);
    // Optional: react to the provider
    // if (descriptor.Provider == DbProvider.PostgreSQL) options.UseNpgsql(...);
    return options.Options;
});
```

### Multi-database

```csharp
// Infrastructure (registered once)
services.AddTarsDataContextAccessor();
services.AddTarsRelationalCompositeConnectionResolver();
services.AddTarsRelationalConfigurationConnectionResolver();
services.AddTarsDataContextFactory();
services.AddTarsRelationalUnitOfWorkFactory();

// Tenant's default database
services.AddTarsData<AppDbContext>("default", (sp, d) =>
    new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(d.ConnectionString).Options);

// Central database (data shared across tenants)
services.AddTarsData<CentralDbContext>("central", (sp, d) =>
    new DbContextOptionsBuilder<CentralDbContext>().UseNpgsql(d.ConnectionString).Options);
```

### Repository registration

```csharp
// Scans the assembly and registers all repositories as Transient
services.AddTarsDataRepositoriesFromAssemblies(typeof(AppAssemblyMarker).Assembly);

// Or by marker type
services.AddTarsDataRepositoriesFromAssemblies(typeof(OrderRepository), typeof(UserRepository));

// Or with a custom lifetime (rarely needed)
services.AddTarsDataRepositoriesFromAssemblies(ServiceLifetime.Scoped, typeof(AppAssemblyMarker).Assembly);
```

### Multi-database coordination

```csharp
// Needed only when using IMultiDatabaseCoordinator directly
services.AddTarsMultiDatabaseCoordination();
```

---

## Custom resolver

For situations where the connection string comes from a secrets vault (Vault, Azure Key Vault, etc.):

```csharp
// Custom implementation
public sealed class VaultDataConnectionResolver : IDataConnectionResolver
{
    private readonly IVaultClient _vault;

    public VaultDataConnectionResolver(IVaultClient vault) => _vault = vault;

    public async Task<IDataConnectionDescriptor?> ResolveAsync(
        DataConnectionResolutionContext ctx,
        CancellationToken ct = default)
    {
        // Return null to let the next resolver in the chain try
        var secret = await _vault.GetSecretAsync($"db/{ctx.DatabaseKey}", ct);
        if (secret is null) return null;

        return new DataConnectionDescriptor
        {
            DatabaseKey = ctx.DatabaseKey,
            ConnectionString = secret.Value,
            Provider = DbProvider.PostgreSQL,
            IsTenantScoped = ctx.TenantKey is not null,
            TenantKey = ctx.TenantKey
        };
    }
}

// Register the custom resolver via TryAddEnumerable to join the chain
services.TryAddEnumerable(ServiceDescriptor.Singleton<IDataConnectionResolver, VaultDataConnectionResolver>());

// Then the normal infrastructure and pipeline
services.AddTarsDataContextAccessor();
services.AddTarsRelationalCompositeConnectionResolver();
services.AddTarsRelationalConfigurationConnectionResolver(); // optional — omit if you don't want config as a fallback
services.AddTarsDataContextFactory();
services.AddTarsRelationalUnitOfWorkFactory();
services.AddTarsData<AppDbContext>(buildOptions);
```

The `CompositeDataConnectionResolver` tries the resolvers in registration order, returning the first non-null result.

---

## DataConnectionResolutionContext

Passed to each `IDataConnectionResolver`:

```csharp
public sealed class DataConnectionResolutionContext
{
    public required string DatabaseKey { get; init; }       // E.g. "default", "central"
    public string? TenantKey { get; init; }                 // E.g. "tenant-a"
    public string? TenantCode { get; init; }                // E.g. "ACME" (readable code)
    public required IServiceProvider ServiceProvider { get; init; }
}
```

`TenantKey` and `TenantCode` are filled in automatically when `Pottmayer.Tars.Multitenancy` is active.
