# Feature Flags Overview

## Projects in this family

- `Pottmayer.Tars.FeatureFlags.Abstractions`
- `Pottmayer.Tars.FeatureFlags`
- `Pottmayer.Tars.FeatureFlags.AspNetCore`

## What the module offers

Global switches that turn a whole module or one capability off **for everyone**, independent of who
the user is. Per-user rules belong to authorization, not here.

- flags **declared in code** by the module that owns them, each with a default and an optional parent
- the declared set validated **at startup**: blank or duplicate keys, unknown parents, parent cycles
- `IFeatureFlags`: `IsEnabledAsync(key)`, `GetAllAsync()` (default, override and effective state of each
  flag) and `Invalidate()`
- `IFeatureFlagStore`: reads the overrides an administrator stored; the application implements it
  (usually on its own database), or uses `InMemoryFeatureFlagStore`
- `[RequiresFeature(key)]` for MVC controllers and actions

A flag is on when its override (or, without one, its default) is on **and** every ancestor is on.
Turning `finances` off turns `finances.import` off with it.

## Registration

There is no all-in-one call. Declare flags once per module, then add the catalog, the service and a
store. `TryAdd` is used throughout, so a store or service registered **before** these calls wins.

```csharp
using Pottmayer.Tars.FeatureFlags.DI;

// In each module that owns flags:
builder.Services.AddTarsFeatureFlagDefinitions(flags => flags
    .Add("finances")
    .Add("finances.import", parent: "finances")
    .Add("finances.open-banking", defaultOn: false, parent: "finances"));

// Once, in the host:
builder.Services.AddTarsFeatureFlagCatalog();       // IFeatureFlagCatalog + startup validation
builder.Services.AddTarsFeatureFlagService();       // IFeatureFlags (singleton, caches the overrides)
builder.Services.AddScoped<IFeatureFlagStore, MyDatabaseFeatureFlagStore>();
// or: builder.Services.AddTarsInMemoryFeatureFlagStore();
```

Every `AddTarsFeatureFlagDefinitions` call adds to the same catalog. Validation runs on host start, so
a typo in a parent key stops the host with a message that names the flag.

## Checking a flag

```csharp
if (!await flags.IsEnabledAsync("finances.import", ct))
    return Error.NotFound(FeatureFlagErrors.Disabled, "Imports are disabled.");
```

An undeclared key throws `InvalidOperationException`: the key is a typo, not a disabled feature.

On controllers, from `Pottmayer.Tars.FeatureFlags.AspNetCore`:

```csharp
[RequiresFeature("finances.import")]
public sealed class ImportsController : ControllerBase { … }
```

While the flag is off the action does not run. The response is a `NotFound` error with code
`feature.disabled`, sent through the registered `IHttpErrorMapper`, so it has the same envelope as any
other Tars error. Several attributes on one target must all be on.

## Storing overrides

`IFeatureFlagStore` only **reads**. Writing an override is the application's job, because it usually
belongs in the same unit of work as an audit entry. After the write commits, call
`IFeatureFlags.Invalidate()`:

```csharp
await repository.SetOverrideAsync("finances.import", false, ct);
await unitOfWork.CommitAsync(ct);
flags.Invalidate();
```

- The store is resolved from a fresh DI scope on every load, so a scoped implementation (an EF Core
  `DbContext`) is fine.
- Overrides for keys that are no longer declared are ignored, and `GetAllAsync` does not list them.
- The cache lives in the process. With several instances, each one keeps its overrides until it is
  invalidated or restarted.

## Testing

Use the in-memory store and set overrides directly:

```csharp
services.AddTarsFeatureFlagDefinitions(f => f.Add("notes"));
services.AddTarsFeatureFlagCatalog();
services.AddTarsFeatureFlagService();
services.AddTarsInMemoryFeatureFlagStore();

var store = provider.GetRequiredService<InMemoryFeatureFlagStore>();
store.Set("notes", false);                     // null removes the override
provider.GetRequiredService<IFeatureFlags>().Invalidate();
```
