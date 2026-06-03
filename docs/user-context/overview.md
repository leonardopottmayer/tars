# User Context — Overview

The User Context module provides access to the current user at any point of the application without coupling the code to `HttpContext` or `ClaimsPrincipal`.

It offers **two parallel and complementary systems**:

| System | Interface | For when |
|---|---|---|
| **Typed** | `IUserContext<TUser>` | The app has a domain user type with typed properties |
| **Claims** | `IUserContext` | You only need basic claims, or: workers, Blazor, tests |

Both can coexist in the same application. There is no conflict — they are distinct contracts.

---

## Packages

| Package | Role |
|---|---|
| `Pottmayer.Tars.UserContext.Abstractions` | Contracts for both systems |
| `Pottmayer.Tars.UserContext` | Runtime implementations (no host dependency) |
| `Pottmayer.Tars.UserContext.AspNetCore` | HTTP adapter: `CurrentPrincipalAccessor` and `UserContextMiddleware` |

---

## Typed system — `IUserContext<TUser>`

Turns the `ClaimsPrincipal` into a strongly-typed domain object.

```csharp
// Definition of the application's user type
public sealed class AppUser
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? TenantId { get; set; }

    [Claim("role")]
    public string? Role { get; set; }

    [Claim("tenant_key")]
    public string? TenantKey { get; set; }
}

// Consumption via dependency injection
public class OrderService(IUserContextAccessor<AppUser> accessor)
{
    public Task PlaceOrderAsync(OrderRequest request)
    {
        var user = accessor.Context.User
            ?? throw new UnauthorizedAccessException();

        var order = new Order(customerId: user.Id, tenantId: user.TenantId!);
        // ...
    }
}
```

**Internal flow:**

```
HttpContext.User (ClaimsPrincipal)
    ↓ ICurrentPrincipalAccessor (AspNetCore)
    ↓ DefaultUserContextFactory<TUser>
    ↓ IUserResolver<TUser> (ClaimsUserResolver or custom)
    → IUserContext<AppUser>
```

---

## Claims system — `IUserContext` (non-generic)

Accesses standard claims directly without needing a domain type. It works in any host — the context is stored in `AsyncLocal<T>` and can be set manually.

```csharp
// Consumption via dependency injection
public class AuditService(IUserContext userContext)
{
    public void Log(string action)
    {
        if (!userContext.IsAuthenticated)
            return;

        _audit.Write(userId: userContext.UserId!, action: action);
    }
}
```

**Internal flow (ASP.NET Core):**

```
HttpContext.User (ClaimsPrincipal)
    ↓ UserContextMiddleware
    ↓ IUserContextAccessor.Current = new UserContext(claims)
    → IUserContext (resolved from the accessor via DI)
```

**Internal flow (worker / test):**

```
accessor.Current = new UserContext([new Claim(ClaimTypes.NameIdentifier, "user-123")])
    → IUserContext (resolved from the accessor via DI)
```

---

## When to use each system

### Use the Typed system when

- You need domain-specific properties (`TenantId`, `Permission`, `Region`, etc.)
- You want the claim → object mapping to be automatic and type-safe
- Your domain code should not know `ClaimTypes` or claim strings

### Use the Claims system when

- You only need `UserId`, `Email`, `Roles` — standard claims
- You are in a worker, background job, Blazor Server or unit test
- You want to set the context manually without configuring the whole resolution pipeline
- You are integrating with a service that has no domain user type

### Use both together when

- The Typed system resolves the full user for the domain layer
- The Claims system feeds cross-cutting infrastructure (auditing, logging, correlation) that does not need the domain type

---

## Documentation by topic

- [Typed system — `IUserContext<TUser>`](./typed-context.md)
- [Claims system — `IUserContext`](./claims-context.md)
- [Configuration and appsettings](./configuration.md)
- [Registration scenarios (DI)](./scenarios.md)
- [Testing](./testing.md)
