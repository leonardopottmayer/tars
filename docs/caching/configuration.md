# Caching Configuration

## `CacheOptions`

Section name:

```json
"Tars": {
  "Caching": {
    "KeyPrefix": "my-app",
    "KeySeparator": ":",
    "DefaultAbsoluteExpirationRelativeToNow": "00:10:00"
  }
}
```

Fields:

- `KeyPrefix`: prefix used by the default key builder. Default: `tars-cache`
- `KeySeparator`: separator. Default: `:`
- `DefaultAbsoluteExpirationRelativeToNow`: default TTL used when the call does not provide an expiration

## `RedisCacheOptions`

Section name:

```json
"Tars": {
  "Caching": {
    "Redis": {
      "ConnectionString": "localhost:6379,abortConnect=False",
      "Database": 0,
      "ClientName": "my-service",
      "AbortOnConnectFail": false,
      "ConnectRetry": 3,
      "ConnectTimeout": "00:00:05",
      "SyncTimeout": "00:00:05",
      "KeepAlive": "00:01:00",
      "AllowAdmin": false
    }
  }
}
```

Relevant fields:

- `ConnectionString`: required
- `Database`: logical database; `null` uses the client default
- `ClientName`: useful for diagnostics in Redis
- `AbortOnConnectFail`: avoids aborting startup on an initial failure
- `ConnectRetry`, `ConnectTimeout`, `SyncTimeout`, `KeepAlive`, `AllowAdmin`

## Important behavior

- `ConnectionMultiplexer` is shared as a singleton
- `IDatabase` is resolved from the multiplexer using the configured `Database`
- the default key builder works well for both memory and Redis

## Usage example

```csharp
public sealed class UserProfileCache
{
    private readonly ICacheService _cache;
    private readonly ICacheKeyBuilder _keys;

    public UserProfileCache(ICacheService cache, ICacheKeyBuilder keys)
    {
        _cache = cache;
        _keys = keys;
    }

    public Task SetAsync(Guid userId, UserDto user, CancellationToken ct)
    {
        var key = _keys.Build("users", userId);
        return _cache.SetAsync(key, user, new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, ct);
    }
}
```
