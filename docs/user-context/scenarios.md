# User Context — Registration Scenarios (DI)

Each scenario shows the complete `Program.cs` and `appsettings.json`.

---

## Scenario 1 — Claims system only (simple API)

No domain user type. Ideal for small APIs or services that only need `UserId` and `Email`.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* JWT options */);

// Claims system
builder.Services.AddTarsUserContextAccessor();   // singleton AsyncLocal
builder.Services.AddTarsUserContext();           // IUserContext transient

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseTarsUserContext();   // ← populates IUserContext from the JWT
app.MapControllers();
app.Run();
```

```json
// appsettings.json — the Claims system does not use the Tars:UserContext section
{
  "Jwt": {
    "Authority": "https://auth.example.com",
    "Audience":  "my-api"
  }
}
```

**Consumption:**

```csharp
[ApiController, Route("orders")]
public class OrdersController(IUserContext userContext) : ControllerBase
{
    [HttpGet]
    public IActionResult GetMyOrders()
    {
        if (!userContext.IsAuthenticated)
            return Unauthorized();

        var orders = _repo.GetByUser(userContext.UserId!);
        return Ok(orders);
    }
}
```

---

## Scenario 2 — Typed system only (API with a rich domain)

A user type with business-specific properties mapped via claims.

```csharp
// AppUser.cs
public sealed class AppUser
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }

    [Claim("tenant_id")]
    public string? TenantId { get; set; }

    [Claim("role")]
    public string? Role { get; set; }
}
```

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* JWT options */);

// Typed system
builder.AddTarsUserContextOptions();
builder.Services.AddTarsClaimsUserResolver<AppUser>();
builder.Services.AddTarsDefaultUserContextFactory<AppUser>();
builder.Services.AddTarsUserContextAccessor<AppUser>();
builder.Services.AddTarsCurrentPrincipalAccessor();

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
// UseTarsUserContext() is not needed — the Typed system uses ICurrentPrincipalAccessor (pull)
app.MapControllers();
app.Run();
```

```json
// appsettings.json
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

**Consumption:**

```csharp
public class TenantOrderService(IUserContextAccessor<AppUser> accessor)
{
    public async Task<List<Order>> GetOrdersAsync()
    {
        var user = accessor.Context.User
            ?? throw new UnauthorizedAccessException();

        return await _repo.GetByTenantAsync(user.TenantId!);
    }
}
```

---

## Scenario 3 — Both systems together (domain + infrastructure)

The Typed system feeds the domain layer. The Claims system feeds logging, auditing and other cross-cutting services.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* JWT options */);

// Typed system
builder.AddTarsUserContextOptions();
builder.Services.AddTarsClaimsUserResolver<AppUser>();
builder.Services.AddTarsDefaultUserContextFactory<AppUser>();
builder.Services.AddTarsUserContextAccessor<AppUser>();
builder.Services.AddTarsCurrentPrincipalAccessor();

// Claims system (for cross-cutting infrastructure)
builder.Services.AddTarsUserContextAccessor();
builder.Services.AddTarsUserContext();

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseTarsUserContext();   // populates IUserContext (Claims)
app.MapControllers();
app.Run();
```

```json
// appsettings.json
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

---

## Scenario 4 — With a fallback provider (mixed authenticated + anonymous API)

```csharp
// GuestUser.cs — default user for anonymous requests
public sealed class AppUser
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool IsGuest { get; set; }
}

// GuestFallbackProvider.cs
public class GuestFallbackProvider : IFallbackUserProvider<AppUser>
{
    public Task<AppUser?> GetFallbackUserAsync(CancellationToken ct = default)
        => Task.FromResult<AppUser?>(new AppUser
        {
            Id      = Guid.Empty,
            Name    = "Guest",
            IsGuest = true,
        });
}
```

```csharp
// Program.cs
builder.AddTarsUserContextOptions();
builder.Services.AddTarsClaimsUserResolver<AppUser>();
builder.Services.AddTarsDefaultUserContextFactory<AppUser>();
builder.Services.AddTarsUserContextAccessor<AppUser>();
builder.Services.AddTarsCurrentPrincipalAccessor();
builder.Services.AddTarsFallbackUserProvider<AppUser, GuestFallbackProvider>();
```

```json
// appsettings.json
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

**Consumption:**

```csharp
public class CatalogService(IUserContextAccessor<AppUser> accessor)
{
    public async Task<List<Product>> GetProductsAsync()
    {
        var user = accessor.Context.User!;   // never null — the fallback guarantees it

        if (user.IsGuest)
            return await _repo.GetPublicProductsAsync();

        return await _repo.GetAllForTenantAsync(user.TenantId!);
    }
}
```

---

## Scenario 5 — Worker with the Claims system

```csharp
// Program.cs (Worker Service)
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTarsUserContextAccessor();
builder.Services.AddTarsUserContext();

builder.Services.AddHostedService<MessageProcessingWorker>();

var host = builder.Build();
host.Run();
```

```csharp
// MessageProcessingWorker.cs
public class MessageProcessingWorker(
    IServiceScopeFactory scopeFactory,
    IUserContextAccessor accessor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var msg in _queue.ReadAllAsync(ct))
        {
            accessor.Current = new UserContext([
                new Claim(ClaimTypes.NameIdentifier, msg.UserId),
                new Claim(ClaimTypes.Name,            msg.UserName),
                new Claim(ClaimTypes.Role,            msg.Role),
                new Claim("tenant_id",                msg.TenantId),
            ]);

            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler>();
            await handler.HandleAsync(msg, ct);

            accessor.Current = null;
        }
    }
}
```

---

## Scenario 6 — Worker with the Typed system (custom resolver)

```csharp
// Program.cs (Worker Service)
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(/* options */);

// Typed system without an HTTP ICurrentPrincipalAccessor — custom accessor
builder.Services.AddSingleton<WorkerPrincipalAccessor>();
builder.Services.AddSingleton<ICurrentPrincipalAccessor>(
    sp => sp.GetRequiredService<WorkerPrincipalAccessor>());

builder.AddTarsUserContextOptions();
builder.Services.AddTarsClaimsUserResolver<AppUser>();
builder.Services.AddTarsDefaultUserContextFactory<AppUser>();
builder.Services.AddTarsUserContextAccessor<AppUser>();

builder.Services.AddHostedService<OrderSyncWorker>();

var host = builder.Build();
host.Run();
```

```csharp
// WorkerPrincipalAccessor.cs
public sealed class WorkerPrincipalAccessor : ICurrentPrincipalAccessor
{
    private static readonly AsyncLocal<ClaimsPrincipal?> _principal = new();

    public ClaimsPrincipal? Principal
    {
        get => _principal.Value;
        set => _principal.Value = value;
    }
}

// OrderSyncWorker.cs
public class OrderSyncWorker(
    IServiceScopeFactory scopeFactory,
    WorkerPrincipalAccessor principalAccessor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var job in _queue.ReadAllAsync(ct))
        {
            principalAccessor.Principal = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, job.UserId),
                    new Claim("tenant_id",               job.TenantId),
                ],
                authenticationType: "worker-job"));

            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<OrderSyncService>();
            await service.SyncAsync(job, ct);

            principalAccessor.Principal = null;
        }
    }
}
```

---

## Scenario 7 — Custom resolver with a database

When the token claims do not contain all the required information — for example, `TenantId` comes from the database and not from the JWT:

```csharp
// DatabaseUserResolver.cs
public sealed class DatabaseUserResolver(AppDbContext db) : IUserResolver<AppUser>
{
    public AppUser Resolve(ClaimsPrincipal principal)
    {
        var id = Guid.Parse(principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Missing sub claim."));

        var user = db.Users
            .Include(u => u.Tenant)
            .FirstOrDefault(u => u.Id == id)
            ?? throw new InvalidOperationException($"User {id} not found.");

        return new AppUser
        {
            Id       = user.Id,
            Name     = user.Name,
            Email    = user.Email,
            TenantId = user.Tenant.Key,
            Role     = user.Role.ToString(),
        };
    }
}

// Registration — do not call AddTarsClaimsUserResolver because the resolver is custom
builder.Services.AddScoped<IUserResolver<AppUser>, DatabaseUserResolver>();
builder.Services.AddTarsDefaultUserContextFactory<AppUser>();
builder.Services.AddTarsUserContextAccessor<AppUser>();
builder.Services.AddTarsCurrentPrincipalAccessor();
```
