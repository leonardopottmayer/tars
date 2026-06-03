# Caching Overview

## Projects in this family

- `Pottmayer.Tars.Caching.Abstractions`
- `Pottmayer.Tars.Caching.Core`
- `Pottmayer.Tars.Caching.Memory`
- `Pottmayer.Tars.Caching.Redis`

## What the module offers

- single `ICacheService` contract
- default key builder with prefix and separator
- default serializer using `System.Text.Json`
- in-memory provider
- Redis provider

## Minimal registration

### Memory cache

```csharp
CoreCachingOptionsDI.AddTarsCachingOptions(builder);
builder.Services.AddTarsCacheKeyBuilder();
builder.Services.AddTarsCacheSerializer();
builder.Services.AddMemoryCache();
builder.Services.AddTarsMemoryCacheProvider();
```

### Redis

```csharp
builder.AddTarsCachingOptions();
builder.AddTarsRedisCachingOptions();
builder.Services.AddTarsCacheKeyBuilder();
builder.Services.AddTarsCacheSerializer();
builder.Services.AddTarsRedisConnectionMultiplexer();
builder.Services.AddTarsRedisDatabase();
builder.Services.AddTarsRedisCacheProvider();
```

## Main contracts

- `ICacheService`
- `ICacheKeyBuilder`
- `ICacheSerializer`
- `CacheEntryOptions`
- `CacheGetResult`

## Configuration

See [configuration.md](./configuration.md).
