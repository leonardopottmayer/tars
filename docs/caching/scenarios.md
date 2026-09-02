# Caching — Registration Scenarios and Testing

Each scenario shows the complete registration. See [overview.md](./overview.md) for the contracts and
[configuration.md](./configuration.md) for the full options reference.

---

## Scenario 1 — Memory only

The default for a single-instance host with no shared-cache requirement.

```csharp
// Program.cs
using Pottmayer.Tars.Caching.DI;
using Pottmayer.Tars.Caching.Memory.DI;
using Pottmayer.Tars.Caching.Memory.Options;

builder.AddTarsMemoryCachingOptions();
builder.Services.AddTarsCacheKeyBuilder<MemoryCachingOptions>();
builder.Services.AddMemoryCache();
builder.Services.AddTarsMemoryCacheProvider();
```

```json
// appsettings.json
{
  "Tars": {
    "Caching": {
      "Memory": {
        "KeyPrefix": "my-app",
        "DefaultAbsoluteExpirationRelativeToNow": "00:10:00"
      }
    }
  }
}
```

---

## Scenario 2 — Redis only (shared cache across instances)

Use when more than one instance of the host runs and cached values must be visible to all of them.

```csharp
// Program.cs
using Pottmayer.Tars.Caching.DI;
using Pottmayer.Tars.Caching.Redis.DI;
using Pottmayer.Tars.Caching.Redis.Options;

builder.AddTarsRedisCachingOptions();
builder.Services.AddTarsCacheKeyBuilder<RedisCachingOptions>();
builder.Services.AddTarsCacheSerializer();
builder.Services.AddTarsRedisConnectionMultiplexer();
builder.Services.AddTarsRedisDatabase();
builder.Services.AddTarsRedisCacheProvider();
```

```json
// appsettings.json
{
  "Tars": {
    "Caching": {
      "Redis": {
        "KeyPrefix": "my-app",
        "ConnectionString": "localhost:6379,abortConnect=False",
        "DefaultAbsoluteExpirationRelativeToNow": "00:10:00"
      }
    }
  }
}
```

---

## Scenario 3 — Read-through cache in front of a repository

The typical use of `GetOrSetAsync`: cache the result of an expensive read, invalidate on write.

```csharp
public sealed class ProductCatalogQueries(
    ICacheStore cache,
    ICacheKeyBuilder keys,
    IProductRepository repository)
{
    public Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct) =>
        cache.GetOrSetAsync(
            keys.Build("products", id),
            async innerCt => (await repository.GetByIdAsync(id, innerCt))?.ToDto(),
            new CacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) },
            ct).AsTask();
}

public sealed class UpdateProductHandler(
    ICacheStore cache,
    ICacheKeyBuilder keys,
    IProductRepository repository)
{
    public async Task HandleAsync(UpdateProductCommand command, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(command.ProductId, ct);
        product!.Rename(command.NewName);
        await repository.UpdateAsync(product, ct);

        // Invalidate — the framework has no automatic write-through, the caller is responsible.
        await cache.RemoveAsync(keys.Build("products", command.ProductId), ct);
    }
}
```

**There is no automatic invalidation.** `ICacheStore` never subscribes to writes on its own — every
write path that changes cached data must call `RemoveAsync` (or overwrite with `SetAsync`) itself. If
several handlers can invalidate the same key, put the invalidation call in one place (a domain-event
handler, a repository decorator) rather than repeating it per command handler.

---

## Testing — faking `ICacheStore`

There is no built-in in-memory fake beyond the real `Pottmayer.Tars.Caching.Memory` provider itself,
which is usually the right choice for tests too — it is fast, requires no external dependency, and
exercises the real key-building and expiration logic:

```csharp
services.AddTarsMemoryCachingOptions(configure: o => o.KeyPrefix = "test");
services.AddTarsCacheKeyBuilder<MemoryCachingOptions>();
services.AddMemoryCache();
services.AddTarsMemoryCacheProvider();
```

For a pure unit test with no DI container, a hand-rolled dictionary-backed fake of `ICacheStore` is
enough — implement `GetOrSetAsync` by checking the dictionary before invoking `factory`, so tests can
assert the factory ran at most once per key.
