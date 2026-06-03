# Data — Overview

The Tars data axis is multi-paradigm. Each paradigm has its own contracts and implementations — there is no universal repository forcing every backend into a single mold.

---

## Packages

### Shared contracts

| Package | Type | Role |
|---|---|---|
| `Pottmayer.Tars.Data.Abstractions` | Abstractions | Provider-agnostic contracts: `IDataContext`, `IDataContextAccessor`, `IRepository`, `IUnitOfWork`, `IUnitOfWorkFactory`, `QueryParams` and query types |

### Relational axis

| Package | Type | Role |
|---|---|---|
| `Pottmayer.Tars.Data.Relational.Abstractions` | Abstractions | Contracts specific to the relational axis: `IStandardRepository`, `IDataContextFactory`, `IDataConnectionResolver`, `IMultiDatabaseCoordinator` |
| `Pottmayer.Tars.Data.Relational` | Runtime | EF Core + Dapper implementation, `RelationalDbContext`, `DataContext`, `StandardRepository`, unified DI |

### Future paradigms

| Package | Status |
|---|---|
| `Data.Document.MongoDB` | Planned (temporarily removed) |
| `Data.Document.CosmosDB` | Planned |
| `Data.KeyValue.Abstractions` / `Data.KeyValue.DynamoDB` | Planned |
| `Data.Search.Abstractions` / `Data.Search.OpenSearch` | Planned |

---

## Core concepts (shared across providers)

These contracts live in `Data.Abstractions` and are implemented by the relational axis (and by future data axes):

| Contract | Role |
|---|---|
| `IUnitOfWorkFactory` | Main entry point in handlers. `ExecuteAsync` creates, runs and disposes the UoW in one line; `Create` exposes the explicit UoW for multi-operation cases. |
| `IUnitOfWork` | Owns an `IDataContext`, commits and disposes automatically. Identical across both providers. |
| `IDataContext` | Boundary of the unit of work. Resolves repositories, commits, collects domain events. |
| `IDataContextAccessor` | Holds the current context during synchronous repository resolution via `AcquireRepository`. Internal detail — it is not used directly by repositories after construction. |
| `IRepository` / `IRepository<T>` | Markers that identify repositories for the DI scanner. |
| `IRepositoryResolver` | Resolves repositories from DI within an active context. |
| `QueryParams` | Dynamic filtering, sorting and pagination. Works across both providers. |
| `DataQueryResult<T>` | Result of `ExecuteQueryAsync` — items + total. |

---

## Minimal registration — relational, single-database app

Each method registers a single service, allowing the application to replace individual components with its own implementations.

```csharp
// Program.cs

// Infrastructure (each method registers one service)
builder.Services.AddTarsDataContextAccessor();
builder.Services.AddTarsRelationalCompositeConnectionResolver();
builder.Services.AddTarsRelationalConfigurationConnectionResolver();
builder.Services.AddTarsDataContextFactory();
builder.Services.AddTarsRelationalUnitOfWorkFactory();

// Database pipeline
builder.Services.AddTarsData<AppDbContext>((sp, descriptor) =>
    new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(descriptor.ConnectionString)
        .Options);

// Repositories
builder.Services.AddTarsDataRepositoriesFromAssemblies(typeof(AppAssemblyMarker).Assembly);
```

```json
// appsettings.json
{
  "Tars": {
    "Data": {
      "Connections": {
        "default": {
          "ConnectionString": "Host=localhost;Database=myapp;Username=postgres;Password=postgres",
          "Provider": "PostgreSQL"
        }
      }
    }
  }
}
```

---

## Topics in this family

- [Configuration](./configuration.md) — appsettings, providers, connection resolution, multitenancy
- [Contracts and Pipelines](./pipelines-and-uow.md) — UnitOfWork, DataContext, repositories, QueryParams, domain events
- [Data, Multitenancy and Multi-Database](./multitenancy-and-multi-database.md) — logical keys, multi-database and transaction coordination
- [Future Paradigms](./future-paradigms.md) — planned document, key-value and search axes

---

## Important notes

- `IUnitOfWork` and `IUnitOfWorkFactory` are provider-agnostic — the application layer does not know which backend is active, which allows reintroducing other data axes without changing the handlers
- Repositories are always **Transient** — they capture the `IDataContext` in the constructor at DI resolution time, and cannot be Singleton
- Domain events are **collected automatically** in the relational provider (EF change tracker); operations outside the change tracker (e.g. Dapper) use `ctx.CollectDomainEvents(aggregate)` manually
- `QueryParams` is translated internally into an expression tree (EF) — dynamic filtering, sorting and pagination
- Domain interfaces (`IUserRepository`, etc.) **do not reference** any provider type — the infrastructure dependency stays in the implementations
