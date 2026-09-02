# Caching Overview

## Projects in this family

- `Pottmayer.Tars.Caching.Abstractions`
- `Pottmayer.Tars.Caching`
- `Pottmayer.Tars.Caching.Memory`
- `Pottmayer.Tars.Caching.Redis`

## What the module offers

- single `ICacheService` contract
- default key builder with prefix and separator
- default serializer using `System.Text.Json`
- in-memory provider
- Redis provider

## Registration

There is no all-in-one call: the consumer wires each piece explicitly. The key builder and serializer are
shared building blocks that live in the core project (`Pottmayer.Tars.Caching.DI.CachingServicesDI`); each
provider adds its own options binder and store.

Registration order does not affect resolution (every registration is a factory resolved when the container
is built), but the sequence below reads in dependency order. `TryAdd` is used throughout, so to swap a piece
(a custom key builder or serializer), register yours **before** these calls.

### Memory cache

```csharp
using Pottmayer.Tars.Caching.DI;             // AddTarsCacheKeyBuilder
using Pottmayer.Tars.Caching.Memory.DI;      // AddTarsMemoryCachingOptions, AddTarsMemoryCacheProvider
using Pottmayer.Tars.Caching.Memory.Options; // MemoryCachingOptions

builder.AddTarsMemoryCachingOptions();                          // 1. bind options (Tars:Caching:Memory)
builder.Services.AddTarsCacheKeyBuilder<MemoryCachingOptions>();  // 2. key builder (reads the bound options)
builder.Services.AddMemoryCache();                              // 3. the underlying IMemoryCache
builder.Services.AddTarsMemoryCacheProvider();                  // 4. the ICacheStore
```

The in-memory store keeps live object references, so it needs no serializer.

### Redis

```csharp
using Pottmayer.Tars.Caching.DI;            // AddTarsCacheKeyBuilder, AddTarsCacheSerializer
using Pottmayer.Tars.Caching.Redis.DI;      // AddTarsRedis* methods
using Pottmayer.Tars.Caching.Redis.Options; // RedisCachingOptions

builder.AddTarsRedisCachingOptions();                          // 1. bind options (Tars:Caching:Redis)
builder.Services.AddTarsCacheKeyBuilder<RedisCachingOptions>();  // 2. key builder (reads the bound options)
builder.Services.AddTarsCacheSerializer();                     // 3. serializer (Redis stores opaque payloads)
builder.Services.AddTarsRedisConnectionMultiplexer();          // 4. shared IConnectionMultiplexer
builder.Services.AddTarsRedisDatabase();                       // 5. IDatabase from the multiplexer
builder.Services.AddTarsRedisCacheProvider();                  // 6. the ICacheStore
```

## Main contracts

- `ICacheService`
- `ICacheKeyBuilder`
- `ICacheSerializer`
- `CacheEntryOptions`
- `CacheGetResult`

## Configuration

See [configuration.md](./configuration.md).
