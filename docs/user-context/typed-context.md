# User Context — Typed System (`IUserContext<TUser>`)

The typed system turns the current `ClaimsPrincipal` into a strongly-typed domain object. The `TUser` type is defined by the application and can have any structure.

---

## Contracts

### `IUserContext<TUser>`

```csharp
public interface IUserContext<out TUser> where TUser : class
{
    bool IsAuthenticated { get; }
    TUser? User { get; }
}
```

### `IUserContextAccessor<TUser>`

```csharp
public interface IUserContextAccessor<TUser> where TUser : class
{
    IUserContext<TUser> Context { get; }
}
```

The accessor is **scoped** and creates the context lazily on the first read of `.Context`.

### `IUserContextFactory<TUser>`

```csharp
public interface IUserContextFactory<TUser> where TUser : class
{
    IUserContext<TUser> Create();
}
```

Responsible for orchestrating resolution: it reads the principal, calls the resolver, applies the fallback.

### `ICurrentPrincipalAccessor`

```csharp
public interface ICurrentPrincipalAccessor
{
    ClaimsPrincipal? Principal { get; }
}
```

Implemented by the host adapter (ASP.NET Core: reads from `IHttpContextAccessor`). In workers, you can implement it directly.

### `IUserResolver<TUser>`

```csharp
public interface IUserResolver<TUser> where TUser : class
{
    TUser Resolve(ClaimsPrincipal principal);
}
```

Maps claims → `TUser`. The framework provides `ClaimsUserResolver<TUser>` (automatic mapping). You can replace it with a custom implementation.

### `IFallbackUserProvider<TUser>`

```csharp
public interface IFallbackUserProvider<TUser> where TUser : class
{
    Task<TUser?> GetFallbackUserAsync(CancellationToken cancellationToken = default);
}
```

Optional. Provides a default user when there is no authenticated principal.

---

## Defining the user type

The `TUser` type can be any class with a parameterless constructor and public properties with a setter:

```csharp
public sealed class AppUser
{
    // Mapping by convention (Id → sub/NameIdentifier, Name → name, Email → email)
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }

    // Explicit mapping by claim name
    [Claim("tenant_id")]
    public string? TenantId { get; set; }

    [Claim("permission_level")]
    public int PermissionLevel { get; set; }

    [Claim("is_internal")]
    public bool IsInternal { get; set; }

    // Claim with the same name as the property (case-insensitive)
    public string? Department { get; set; }
}
```

---

## Claim mapping

### By convention

`ClaimsUserResolver<TUser>` applies the following conventions when there is no `[Claim]`:

| Property name | Claims tried (in order) |
|---|---|
| `Id` | `ClaimTypes.NameIdentifier`, `sub`, `Id` |
| `Name` | `ClaimTypes.Name`, `name`, `Name` |
| `Email` | `ClaimTypes.Email`, `email`, `Email` |
| any other | exactly the property name |

### With `[Claim]`

```csharp
[Claim("tenant_key")]          // maps from the "tenant_key" claim
public string? TenantKey { get; set; }

[Claim("http://schemas.custom/role")]   // full URIs are valid
public string? CustomRole { get; set; }
```

### Supported conversions

The resolver converts claim strings to the .NET types below (including `Nullable<T>`):

| .NET type | Claim example |
|---|---|
| `string` | `"admin"` |
| `Guid` | `"3fa85f64-5717-4562-b3fc-2c963f66afa6"` |
| `int`, `long`, `short`, `byte` | `"42"` |
| `bool` | `"true"` |
| `double`, `decimal` | `"3.14"` |
| `DateTime` | `"2024-01-15T10:30:00Z"` |
| `DateTimeOffset` | `"2024-01-15T10:30:00+03:00"` |
| `enum` | `"Admin"` (case-insensitive) |

---

## Registration in DI

### Minimal registration (ASP.NET Core)

```csharp
// Program.cs
builder.AddTarsUserContextOptions();                            // reads appsettings
builder.Services.AddTarsClaimsUserResolver<AppUser>();         // automatic resolver
builder.Services.AddTarsDefaultUserContextFactory<AppUser>();  // default factory
builder.Services.AddTarsUserContextAccessor<AppUser>();        // scoped accessor
builder.Services.AddTarsCurrentPrincipalAccessor();            // reads HttpContext.User
```

### With a custom resolver

When the automatic mapping is not enough — for example, when the user needs data from the database:

```csharp
// Implement IUserResolver<TUser>
public class DatabaseUserResolver : IUserResolver<AppUser>
{
    public DatabaseUserResolver(AppDbContext db) => _db = db;

    public AppUser Resolve(ClaimsPrincipal principal)
    {
        var id = Guid.Parse(principal.FindFirstValue("sub")!);
        return _db.Users.Find(id) ?? throw new InvalidOperationException("User not found.");
    }
}

// Registration
builder.Services.AddScoped<IUserResolver<AppUser>, DatabaseUserResolver>();
builder.Services.AddTarsDefaultUserContextFactory<AppUser>();
builder.Services.AddTarsUserContextAccessor<AppUser>();
builder.Services.AddTarsCurrentPrincipalAccessor();
```

> Do not call `AddTarsClaimsUserResolver<AppUser>()` when registering a custom resolver.

### With a fallback provider

```csharp
// Option 1: Class
public class SystemUserProvider : IFallbackUserProvider<AppUser>
{
    public Task<AppUser?> GetFallbackUserAsync(CancellationToken ct = default)
        => Task.FromResult<AppUser?>(new AppUser { Id = Guid.Empty, Name = "System" });
}
services.AddTarsFallbackUserProvider<AppUser, SystemUserProvider>();

// Option 2: Synchronous delegate
services.AddTarsFallbackUserProvider<AppUser>(
    () => new AppUser { Id = Guid.Empty, Name = "Anonymous" });

// Option 3: Asynchronous delegate
services.AddTarsFallbackUserProvider<AppUser>(
    async ct => await _db.GetSystemUserAsync(ct));
```

---

## Usage in services

### Consuming `IUserContextAccessor<TUser>`

```csharp
public class OrderService(IUserContextAccessor<AppUser> accessor)
{
    public async Task<Order> PlaceOrderAsync(OrderRequest request)
    {
        var ctx = accessor.Context;

        if (!ctx.IsAuthenticated || ctx.User is null)
            throw new UnauthorizedAccessException("Login required.");

        return new Order
        {
            CustomerId = ctx.User.Id,
            TenantId   = ctx.User.TenantId!,
            Items      = request.Items,
        };
    }
}
```

### Consuming `IUserContext<TUser>` directly

Register it as transient to inject it directly:

```csharp
// Additional registration
services.AddTransient<IUserContext<AppUser>>(sp =>
    sp.GetRequiredService<IUserContextAccessor<AppUser>>().Context);

// Consumption
public class ReportService(IUserContext<AppUser> userContext)
{
    public Report Generate()
    {
        if (!userContext.IsAuthenticated)
            return Report.Empty;

        return new Report(owner: userContext.User!.Name!);
    }
}
```

---

## Complete resolution flow

```
HTTP request arrives
    │
    ▼
ICurrentPrincipalAccessor.Principal
    → CurrentPrincipalAccessor (AspNetCore)
    → IHttpContextAccessor.HttpContext?.User
    │
    ▼ (if null or not authenticated)
UseFallbackUserWhenAnonymous?
    → yes: IFallbackUserProvider<TUser>.GetFallbackUserAsync()
    → no: UserContext<TUser>(false, null)
    │
    ▼ (if authenticated)
GetUserId() — looks in: NameIdentifier, sub, uid, user_id
    → empty + ThrowOnMissingRequiredUserId: InvalidOperationException
    → empty + no throw: tries the fallback or returns anonymous
    │
    ▼
IUserResolver<TUser>.Resolve(principal)
    → ClaimsUserResolver: reads claims and maps properties
    → custom resolver: your logic
    │
    ▼
UserContext<TUser>(isAuthenticated: true, user: resolvedUser)
```

---

## Implementing `ICurrentPrincipalAccessor` in workers

When there is no HTTP, implement the contract directly:

```csharp
// For workers where the principal comes from elsewhere
public class WorkerPrincipalAccessor : ICurrentPrincipalAccessor
{
    private static readonly AsyncLocal<ClaimsPrincipal?> _principal = new();

    public ClaimsPrincipal? Principal
    {
        get => _principal.Value;
        set => _principal.Value = value;
    }
}

// Registration
services.AddSingleton<ICurrentPrincipalAccessor, WorkerPrincipalAccessor>();

// Usage in the worker before dispatching to the domain
var accessor = sp.GetRequiredService<ICurrentPrincipalAccessor>() as WorkerPrincipalAccessor;
accessor!.Principal = new ClaimsPrincipal(new ClaimsIdentity(
[
    new Claim(ClaimTypes.NameIdentifier, message.UserId),
    new Claim(ClaimTypes.Name, message.Username),
], authenticationType: "worker"));
```
