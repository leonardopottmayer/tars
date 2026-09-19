# Data — Future Paradigms

The Tars data axis is multi-paradigm. The **relational** axis (EF Core + Dapper) and the **document** axis
(MongoDB) are implemented; this page covers the paradigms that are still planned, plus how any mix of axes
coexists in one application.

---

## Document family (implemented)

The MongoDB provider is implemented — see [MongoDB provider](./document-mongodb.md). It does **not** get its
own repository contract: it implements the same provider-agnostic `IStandardRepository<TEntity, TKey>` as the
relational axis, so domain repository interfaces (`IUserRepository`) stay backend-neutral and never name a
provider. The relational axis adds `IRelationalRepository` (with `Queryable()`) only for callers that
deliberately need composable EF `IQueryable`. CosmosDB is planned as a second document provider.

---

## Key-Value / Wide-Column family (planned)

Do not force DynamoDB into `IStandardRepository`. Each paradigm has its own contract.

### Planned contracts

```csharp
// Simple key-value (Redis, DynamoDB simple mode)
public interface IKeyValueStore : IAsyncDisposable
{
    string Name { get; }
    Task<TValue?> GetAsync<TValue>(string key, CancellationToken ct = default);
    Task SetAsync<TValue>(string key, TValue value, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}

// Partitioned (DynamoDB, Cassandra)
public interface IPartitionedStore : IAsyncDisposable
{
    string Name { get; }
    Task<TItem?> GetAsync<TItem>(string partitionKey, string sortKey, CancellationToken ct = default);
    Task PutAsync<TItem>(string partitionKey, string sortKey, TItem item, CancellationToken ct = default);
    Task DeleteAsync(string partitionKey, string sortKey, CancellationToken ct = default);
    Task<IReadOnlyList<TItem>> QueryByPartitionAsync<TItem>(string partitionKey, CancellationToken ct = default);
}

// TTL
public interface ITimeToLiveStore
{
    Task SetWithTtlAsync<TValue>(string key, TValue value, TimeSpan ttl, CancellationToken ct = default);
    Task<TimeSpan?> GetTtlAsync(string key, CancellationToken ct = default);
}
```

---

## Search family (planned)

Do not force OpenSearch/Elasticsearch into `IDocumentCollection`.

### Planned contracts

```csharp
public interface ISearchIndex<TDocument> where TDocument : class
{
    string IndexName { get; }
    Task<ISearchResult<TDocument>> SearchAsync(ISearchQuery query, CancellationToken ct = default);
    Task<TDocument?> GetByIdAsync(string id, CancellationToken ct = default);
}

public interface IIndexWriter<TDocument> where TDocument : class
{
    Task IndexAsync(TDocument document, CancellationToken ct = default);
    Task IndexManyAsync(IEnumerable<TDocument> documents, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
```

---

## Coexistence without conflict

The shared runtime (`Pottmayer.Tars.Data`) is registered **once**, and each provider contributes its own
**keyed pipelines** on top. Every pipeline registers an `IKeyedDataContextFactory` for its `databaseKey`, and
the single composite `IDataContextFactory` routes each key to the provider that owns it — so relational and
document databases coexist behind one `IUnitOfWorkFactory`:

```csharp
// Shared, provider-agnostic infrastructure (registered once)
services.AddTarsDataContextAccessor();
services.AddTarsDataContextFactory();
services.AddTarsUnitOfWorkFactory();

// Relational keys
services.AddTarsRelationalCompositeConnectionResolver();
services.AddTarsRelationalConfigurationConnectionResolver();
services.AddTarsRelationalData<AppDbContext>("sql-central", (sp, d) => /* UseNpgsql */ default!);

// Document keys — same IUnitOfWorkFactory, different backend
services.AddTarsMongoCompositeConnectionResolver();
services.AddTarsMongoConfigurationConnectionResolver();
services.AddTarsMongoData("mongo-catalog");
services.AddTarsMongoData("mongo-events");

// Key-Value — independent entry point (future)
// services.AddTarsDynamoDbStore(opts => { ... });

// Search — independent entry point (future)
// services.AddTarsOpenSearchIndex<ProductDocument>("products-index", opts => { ... });
```

Any mix works — 2 SQL + 1 Mongo, 2 Mongo + 1 SQL, 2 of each — and every key is multitenant on its own terms.
`_uowFactory.Create("sql-central")` and `_uowFactory.Create("mongo-catalog")` live side by side, and the
`IMultiDatabaseCoordinator` can commit across providers (best-effort, Level 1).

> **Note:** the public contracts (`IUnitOfWork`, `IDataContext`, `IStandardRepository`) are shared, so
> switching a key from one provider to another means changing its pipeline registration
> (`AddTarsRelationalData` → `AddTarsMongoData`) and swapping the concrete repository implementation for that
> key — the **domain repository interface stays identical**. Earlier drafts of this framework assumed an app
> would pick a single provider; the keyed/composite design supersedes that — providers coexist.
