# Data — Future Paradigms

The Tars data axis is multi-paradigm. Currently only the **relational** axis (EF Core + Dapper) is implemented. This page covers the paradigms that are still planned.

---

## Document family (planned)

The document axis (MongoDB and, in the future, CosmosDB) should not be forced into the relational `IStandardRepository` — each paradigm has its own contract (`IMongoStandardRepository`, etc.). MongoDB support was temporarily removed and will return as a dedicated document family.

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

Each paradigm has an independent entry point in the container:

```csharp
// Relational — via IUnitOfWorkFactory + IDataContextFactory
services.AddTarsData<AppDbContext>(buildOptions);

// Document — independent entry point (future)
// services.AddTarsMongoData("catalog");

// Key-Value — independent entry point (future)
services.AddTarsDynamoDbStore(opts => { ... });

// Search — independent entry point (future)
services.AddTarsOpenSearchIndex<ProductDocument>("products-index", opts => { ... });
```

> **Note:** when a document axis is reintroduced, the public contracts (`IUnitOfWork`, `IDataContext`) will tend to be identical to the relational ones, but an application must choose **a single data provider**. Each provider registers its own implementation of `IDataContextAccessor` and `IUnitOfWorkFactory`, and the container would resolve only one of them. The design ensures that switching between providers does not require changes in the application layer.
