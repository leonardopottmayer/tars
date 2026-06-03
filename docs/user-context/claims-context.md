# User Context — Claims System (`IUserContext`)

The claims system directly exposes the standard claims of the current user without requiring a domain type. The context is stored in `AsyncLocal<T>`, working identically in ASP.NET Core, workers, Blazor Server and unit tests.

---

## Contracts

### `IUserContext`

```csharp
public interface IUserContext
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<Claim> Claims { get; }
    bool IsInRole(string role);
    string? GetClaim(string claimType);
}
```

### `IUserContextAccessor`

```csharp
public interface IUserContextAccessor
{
    IUserContext? Current { get; set; }
}
```

The accessor is **singleton** and stores the value per async execution context (`AsyncLocal<T>`). Each request (or each worker operation) has its own isolated value.

---

## Implementations

### `UserContext`

Immutable implementation built from a list of claims:

```csharp
var ctx = new UserContext(principal.Claims.ToList());

// Or manually:
var ctx = new UserContext([
    new Claim(ClaimTypes.NameIdentifier, "user-123"),
    new Claim(ClaimTypes.Email, "user@example.com"),
    new Claim(ClaimTypes.Role, "admin"),
]);
```

Automatic mapping of the properties:

| Property | Claims tried (in order) |
|---|---|
| `UserId` | `ClaimTypes.NameIdentifier`, then `sub` |
| `Username` | `ClaimTypes.Name`, then `name` |
| `Email` | `ClaimTypes.Email`, then `email` |
| `Roles` | all claims with type `ClaimTypes.Role` |

### `UserContext.Anonymous`

A pre-built context for unauthenticated users — used by DI when no context has been set:

```csharp
// IsAuthenticated = false, all properties are null/empty
var anon = UserContext.Anonymous;
```

### `AsyncLocalUserContextAccessor`

Stores `IUserContext?` in `AsyncLocal<T>`. Singleton, thread-safe, isolated per async context:

```csharp
public sealed class AsyncLocalUserContextAccessor : IUserContextAccessor
{
    private static readonly AsyncLocal<IUserContext?> _current = new();

    public IUserContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
```

---

## Registration in DI

```csharp
// Registers IUserContextAccessor → AsyncLocalUserContextAccessor (singleton)
services.AddTarsUserContextAccessor();

// Registers IUserContext as transient:
// returns accessor.Current ?? UserContext.Anonymous
services.AddTarsUserContext();
```

In ASP.NET Core, also add the middleware to populate the context on requests:

```csharp
// In Program.cs — after UseAuthentication()
app.UseTarsUserContext();
```

---

## `UserContextMiddleware`

The middleware sets `IUserContextAccessor.Current` from `HttpContext.User` at the start of each request and clears it at the end:

```csharp
public async Task InvokeAsync(HttpContext context, IUserContextAccessor accessor)
{
    if (context.User.Identity?.IsAuthenticated == true)
        accessor.Current = new UserContext(context.User.Claims.ToList());

    try
    {
        await _next(context);
    }
    finally
    {
        accessor.Current = null;   // avoids leaking into the thread pool
    }
}
```

**Position in the pipeline:** it must come **after** `UseAuthentication()` and `UseAuthorization()` so that `HttpContext.User` is already populated:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseTarsUserContext();   // ← here
app.MapControllers();
```

---

## Usage in services

### Consuming `IUserContext` directly

```csharp
public class AuditService(IUserContext userContext, IAuditRepository repo)
{
    public async Task RecordAsync(string action)
    {
        await repo.InsertAsync(new AuditEntry
        {
            UserId    = userContext.UserId ?? "anonymous",
            Action    = action,
            Timestamp = DateTimeOffset.UtcNow,
        });
    }
}
```

### Checking authentication

```csharp
public class OrderService(IUserContext userContext)
{
    public Task PlaceOrderAsync(OrderRequest request)
    {
        if (!userContext.IsAuthenticated)
            throw new UnauthorizedAccessException();

        var order = new Order(
            customerId: userContext.UserId!,
            items:      request.Items);

        // ...
    }
}
```

### Checking roles

```csharp
public class AdminService(IUserContext userContext)
{
    public void ExecuteAdminAction()
    {
        if (!userContext.IsInRole("admin"))
            throw new ForbiddenException();

        // ...
    }
}
```

### Reading custom claims

```csharp
public class TenantService(IUserContext userContext)
{
    public string GetTenantId()
        => userContext.GetClaim("tenant_id")
           ?? throw new InvalidOperationException("tenant_id claim missing.");
}
```

### Consuming via `IUserContextAccessor`

Useful when you need to check whether there is a context before acting:

```csharp
public class BackgroundNotifier(IUserContextAccessor accessor)
{
    public void Notify(string message)
    {
        var ctx = accessor.Current;
        if (ctx is null || !ctx.IsAuthenticated)
            return;   // anonymous operation, no notification

        _notifier.Send(ctx.UserId!, message);
    }
}
```

---

## Usage in workers and background services

Without HTTP middleware, you set the context manually before dispatching to the domain:

```csharp
public class OrderProcessingWorker(
    IServiceScopeFactory scopeFactory,
    IUserContextAccessor accessor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var message in _queue.ReadAllAsync(ct))
        {
            // Set the context before processing
            accessor.Current = new UserContext([
                new Claim(ClaimTypes.NameIdentifier, message.UserId),
                new Claim(ClaimTypes.Name,            message.UserName),
                new Claim(ClaimTypes.Role,            message.Role),
            ]);

            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IOrderHandler>();
            await handler.HandleAsync(message, ct);

            accessor.Current = null;   // clear after processing
        }
    }
}
```

---

## Usage in Blazor Server

In Blazor Server, `HttpContext` is not available during component interaction. Set the context at the start of the circuit:

```csharp
// In _Host.cshtml or in the root component (App.razor)
@inject IUserContextAccessor UserContextAccessor
@inject AuthenticationStateProvider AuthState

@code {
    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
        {
            UserContextAccessor.Current = new UserContext(
                state.User.Claims.ToList());
        }
    }
}
```

---

## Combining with the Typed system

The two systems are independent. You can have `IUserContext<AppUser>` for the domain layer and `IUserContext` for cross-cutting infrastructure:

```csharp
// The domain layer uses the full user type
public class OrderService(IUserContextAccessor<AppUser> accessor)
{
    public Task PlaceOrderAsync(OrderRequest request)
    {
        var user = accessor.Context.User!;
        return _repo.InsertAsync(new Order
        {
            CustomerId = user.Id,
            TenantId   = user.TenantId!,
        });
    }
}

// Infrastructure uses flat claims — it does not need to know AppUser
public class RequestLogger(IUserContext userContext, ILogger<RequestLogger> logger)
{
    public void Log(string path)
        => logger.LogInformation("User {UserId} accessed {Path}",
               userContext.UserId ?? "anon", path);
}
```
