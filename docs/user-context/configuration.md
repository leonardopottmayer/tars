# User Context — Configuration

## Configuration section

The typed system options live under:

```json
"Tars": {
  "UserContext": { ... }
}
```

The claims (non-generic) system has no `appsettings` options — its configuration is done entirely in DI.

---

## `UserContextOptions`

Registration:

```csharp
builder.AddTarsUserContextOptions();

// With a programmatic override
builder.AddTarsUserContextOptions(configure: opts =>
{
    opts.ThrowOnConversionError         = false;
    opts.ThrowOnMissingRequiredUserId   = false;
    opts.UseFallbackUserWhenAnonymous   = true;
});

// With a custom section name
builder.AddTarsUserContextOptions(sectionName: "MyApp:UserContext");
```

### Fields

| Field | Type | Default | Description |
|---|---|---|---|
| `ThrowOnConversionError` | `bool` | `true` | If `true`, a failure converting a claim → property throws `InvalidOperationException`. If `false`, the property keeps the type's default value. |
| `ThrowOnMissingRequiredUserId` | `bool` | `true` | If `true`, an authenticated principal without an identification claim (`sub`, `NameIdentifier`, `uid`, `user_id`) throws `InvalidOperationException`. |
| `UseFallbackUserWhenAnonymous` | `bool` | `true` | If `true`, the factory calls `IFallbackUserProvider<TUser>` when there is no authenticated principal or the userId is missing. |

---

## `appsettings` examples

### Scenario 1 — Production (restrictive behavior)

```json
{
  "Tars": {
    "UserContext": {
      "ThrowOnConversionError": true,
      "ThrowOnMissingRequiredUserId": true,
      "UseFallbackUserWhenAnonymous": false
    }
  }
}
```

Suitable for APIs where every request must be authenticated. Claim errors expose configuration problems early.

---

### Scenario 2 — Mixed API (authenticated + anonymous)

```json
{
  "Tars": {
    "UserContext": {
      "ThrowOnConversionError": true,
      "ThrowOnMissingRequiredUserId": false,
      "UseFallbackUserWhenAnonymous": true
    }
  }
}
```

Public endpoints return anonymous content. `ThrowOnMissingRequiredUserId: false` allows continuing even without `sub`. The fallback provider supplies a default user (guest/anonymous).

---

### Scenario 3 — Development

```json
{
  "Tars": {
    "UserContext": {
      "ThrowOnConversionError": false,
      "ThrowOnMissingRequiredUserId": false,
      "UseFallbackUserWhenAnonymous": true
    }
  }
}
```

Permissive to make local testing easier without authentication configured. The fallback provider can return a fixed development user.

---

### Scenario 4 — Internal job / worker

```json
{
  "Tars": {
    "UserContext": {
      "ThrowOnConversionError": true,
      "ThrowOnMissingRequiredUserId": false,
      "UseFallbackUserWhenAnonymous": true
    }
  }
}
```

Workers operate with a system identity (via the fallback provider). There is no HTTP, so `ThrowOnMissingRequiredUserId: false` avoids failures when the principal is null.

---

## Conversions supported by `ClaimsUserResolver`

The resolver supports the types below, including `Nullable<T>`:

| .NET type | Expected format in the claim |
|---|---|
| `string` | any text |
| `Guid` | `"3fa85f64-5717-4562-b3fc-2c963f66afa6"` |
| `int` | `"42"` |
| `long` | `"9876543210"` |
| `short` | `"100"` |
| `byte` | `"255"` |
| `bool` | `"true"` or `"false"` |
| `double` | `"3.14"` (invariant culture) |
| `decimal` | `"19.99"` (invariant culture) |
| `DateTime` | ISO 8601 round-trip |
| `DateTimeOffset` | ISO 8601 with offset |
| `enum` (any) | the value name, case-insensitive |

---

## Lifetimes

### Typed system

| Service | Lifetime | Reason |
|---|---|---|
| `ICurrentPrincipalAccessor` | Scoped | Reads `HttpContext`, which is per-request |
| `IUserResolver<TUser>` | Scoped | May have scoped dependencies (e.g. `DbContext`) |
| `IUserContextFactory<TUser>` | Scoped | Depends on `ICurrentPrincipalAccessor` and `IUserResolver<TUser>` |
| `IUserContextAccessor<TUser>` | Scoped | Lazy cache of the context per request |
| `IFallbackUserProvider<TUser>` | Scoped | May need scoped repositories |

### Claims system

| Service | Lifetime | Reason |
|---|---|---|
| `IUserContextAccessor` | Singleton | `AsyncLocal<T>` is per execution context, not per instance |
| `IUserContext` | Transient | Read from the accessor at resolution time |

---

## Fallback provider

The fallback is only invoked when:
1. `UseFallbackUserWhenAnonymous: true` in the options
2. The principal is `null`, not authenticated, or the userId is missing
3. An `IFallbackUserProvider<TUser>` was registered in DI

### Implementation

```csharp
public class DevFallbackUserProvider : IFallbackUserProvider<AppUser>
{
    public Task<AppUser?> GetFallbackUserAsync(CancellationToken ct = default)
        => Task.FromResult<AppUser?>(new AppUser
        {
            Id   = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Dev User",
        });
}
```

### Registration

```csharp
// Via class
services.AddTarsFallbackUserProvider<AppUser, DevFallbackUserProvider>();

// Via synchronous delegate
services.AddTarsFallbackUserProvider<AppUser>(
    () => new AppUser { Id = Guid.Empty, Name = "Anonymous" });

// Via asynchronous delegate
services.AddTarsFallbackUserProvider<AppUser>(async ct =>
{
    var user = await _db.GetSystemUserAsync(ct);
    return user;
});
```

> The fallback is registered as **scoped**, so it can inject `DbContext` and other scoped services.
