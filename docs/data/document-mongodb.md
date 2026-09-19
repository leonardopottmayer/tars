# Data — MongoDB Provider (Document Axis)

The document axis implements the **same provider-agnostic contracts** as the relational axis, so application
code — handlers, domain repository interfaces, units of work — is identical whether a key is backed by
PostgreSQL or MongoDB. This page covers how to register it, how its unit of work and transactions behave,
how to write repositories, multitenancy, and how it coexists with the relational axis.

> Related pages:
> - [Data — Overview](./overview.md)
> - [Data — Contracts and UoW](./pipelines-and-uow.md)
> - [Data, Multitenancy and Multi-Database](./multitenancy-and-multi-database.md)

---

## 1. Packages

| Package | Type | Role |
|---|---|---|
| `Pottmayer.Tars.Data.Abstractions` | Abstractions | Provider-agnostic contracts (`IStandardRepository`, `IUnitOfWork`, `IDataContext`, …) |
| `Pottmayer.Tars.Data` | Runtime | Shared runtime (accessor, composite factory, unit of work, coordination) |
| `Pottmayer.Tars.Data.Document.Abstractions` | Abstractions | Mongo connection descriptor, resolution context and `IMongoConnectionResolver` |
| `Pottmayer.Tars.Data.Document.MongoDB` | Runtime | `MongoDataContext`, `MongoStandardRepository`, connection resolution, DI |

The MongoDB provider builds on the [MongoDB.Driver](https://www.nuget.org/packages/MongoDB.Driver).

---

## 2. Why there is no `IMongoRepository`

The provider deliberately does **not** introduce a Mongo-specific repository contract. `IStandardRepository<TEntity, TKey>`
is expressed entirely in BCL types (`Expression<Func<T, bool>>`, `QueryParams`), so MongoDB implements it exactly
as EF Core does. A domain interface stays neutral:

```csharp
// Domain — no provider name in sight, portable across backends
public interface IProductRepository : IStandardRepository<Product, string>;
```

Switching that repository from relational to MongoDB means changing **which pipeline owns the key** and
**which concrete implementation is registered** — never the domain interface. The relational-only surface
(`Queryable()`, raw EF `IQueryable`) lives on `IRelationalRepository`, which a repository opts into only when
it truly needs composable LINQ.

---

## 3. Registration

The shared infrastructure is registered once; the MongoDB pipeline is added per database key.

```csharp
// Program.cs

// Shared, provider-agnostic infrastructure
builder.Services.AddTarsDataContextAccessor();
builder.Services.AddTarsDataContextFactory();
builder.Services.AddTarsUnitOfWorkFactory();

// MongoDB connection resolution + pipelines
builder.Services.AddTarsMongoCompositeConnectionResolver();
builder.Services.AddTarsMongoConfigurationConnectionResolver();
builder.Services.AddTarsMongoData("catalog");     // named key
builder.Services.AddTarsMongoData();              // or the "default" key

// Repositories (scanned; the same scanner serves every provider)
builder.Services.AddTarsDataRepositoriesFromAssemblies(typeof(AppAssemblyMarker).Assembly);
```

```json
// appsettings.json
{
  "Tars": {
    "Data": {
      "Mongo": {
        "Connections": {
          "catalog": {
            "ConnectionString": "mongodb://localhost:27017/?replicaSet=rs0",
            "Database": "catalog"
          }
        }
      }
    }
  }
}
```

Connection resolution mirrors the relational precedence, but resolves a database **name** instead of a
`DbProvider`:

1. `Tars:Data:Mongo:TenantConnections:{key}:{tenantKey}` — explicit per-tenant connection (only with a tenant);
2. `Tars:Data:Mongo:TenantConnectionTemplates:{key}` — template with `{tenantKey}` / `{tenantCode}` placeholders,
   applied to both `Template` (connection string) and `Database`;
3. `Tars:Data:Mongo:Connections:{key}` — static/shared connection.

Each entry provides a `ConnectionString` and a `Database`.

---

## 4. Unit of work, transactions and read-your-writes

`MongoDataContext` runs every read and write through a single `IClientSessionHandle` with an **open transaction**,
started lazily on first use:

- reads see this unit's own uncommitted writes (**read-your-writes**);
- `CommitAsync` flushes them **atomically**; disposing without committing **aborts** the transaction.

```csharp
await _uowFactory.ExecuteAsync("catalog", async (ctx, ct) =>
{
    var products = ctx.AcquireRepository<IProductRepository>();
    await products.AddAsync(product, ct);
    // read-your-writes: the just-added product is visible in the same unit of work
    var again = await products.GetByIdAsync(product.Id, ct);
});
// committed here; a thrown exception instead would abort the transaction — nothing persists
```

> **Replica set required.** MongoDB transactions only work against a replica set (a single-node replica set is
> enough for local/dev). A standalone `mongod` will fail when the transaction starts. Point the connection
> string at a replica set (e.g. `?replicaSet=rs0`).

---

## 5. Writing a repository

Concrete repositories extend `MongoStandardRepository<TEntity, TKey>` and supply the key member via
`IdSelector`. Override `CollectionName` and `AllowedQueryFields` as needed.

```csharp
public sealed class ProductRepository(IDataContextAccessor accessor)
    : MongoStandardRepository<Product, string>(accessor), IProductRepository
{
    protected override string CollectionName => "products";
    protected override Expression<Func<Product, string>> IdSelector => p => p.Id;

    // opt into dynamic filtering/sorting for ExecuteQueryAsync
    protected override IReadOnlySet<string> AllowedQueryFields { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { nameof(Product.Name), nameof(Product.Price) };
}
```

`ExecuteQueryAsync(QueryParams)` works identically to the relational axis: the same `QueryParams` DSL is
translated once into an `Expression` predicate plus sort/paging and applied to the collection.

Map the document's id to `_id` with `[BsonId]`, and annotate `decimal` with `[BsonRepresentation(BsonType.Decimal128)]`:

```csharp
public sealed class Product
{
    [BsonId] public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    [BsonRepresentation(BsonType.Decimal128)] public decimal Price { get; set; }
}
```

---

## 6. Domain events

MongoDB has no change tracker, so aggregates are collected explicitly. `MongoStandardRepository` automatically
registers any entity implementing `IHasDomainEvents` when it is added, updated or removed; for writes made
outside the repository, call `ctx.CollectDomainEvents(aggregate)`. Events are dispatched **inside the
transaction, before commit** — an outbox-style handler that writes through the same context joins the
transaction, so the aggregate change and its integration event commit together (or neither does).

---

## 7. Multitenancy

Tenant resolution is unchanged: the ambient `ITenantContext` (from `Pottmayer.Tars.Multitenancy`) feeds the
Mongo connection resolver, exactly as it does the relational one. Database-per-tenant via a template:

```json
{
  "Tars": { "Data": { "Mongo": {
    "TenantConnectionTemplates": {
      "primary": {
        "Template": "mongodb://mongo:27017/?replicaSet=rs0",
        "Database": "tenant_{tenantKey}"
      }
    }
  } } }
}
```

With tenant `acme` resolved, `_uowFactory.Create("primary")` opens `tenant_acme`; with `globex`, `tenant_globex` —
isolated, with no code change. This works in HTTP and, via `ITenantExecutionRunner`, in jobs and workers.

---

## 8. Coexistence with the relational axis

A MongoDB key and a relational key resolve through the same composite factory, so one application can register
any mix and drive them all from one `IUnitOfWorkFactory`:

```csharp
services.AddTarsRelationalData<AuditDbContext>("audit", (_, d) =>
    new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(d.ConnectionString).Options);
services.AddTarsMongoData("catalog");

// ... later
await _coordinator.ExecuteAsync(["catalog", "audit"], async (mdb, ct) =>
{
    var mongo = await mdb.GetUnitOfWork("catalog").GetContextAsync(ct);
    var sql   = await mdb.GetUnitOfWork("audit").GetContextAsync(ct);
    await mongo.AcquireRepository<IProductRepository>().AddAsync(product, ct);
    await sql.AcquireRepository<IAuditRepository>().AddAsync(audit, ct);
    // the coordinator commits each unit of work in sequence (best-effort, Level 1)
});
```

See [Data, Multitenancy and Multi-Database](./multitenancy-and-multi-database.md) for the coordination levels.

---

## 9. Limitations and notes

- **Replica set only** — transactions require it (single node is fine for dev).
- **Best-effort cross-provider commit** — the coordinator is Level 1 (sequential, no distributed atomicity).
  For strong cross-store consistency, prefer an outbox.
- **Key ops need `IdSelector`** — the concrete repository declares the key member; there is no EF-style
  automatic key discovery.
- **`MongoClient` is pooled** — one client per connection string is reused across the app (it is thread-safe
  and owns its connection pool).
