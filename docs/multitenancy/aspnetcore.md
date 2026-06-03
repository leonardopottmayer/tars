# Multitenancy in ASP.NET Core

## Package

`Pottmayer.Tars.Multitenancy.AspNetCore`

---

## Middleware

`UseTarsTenantResolution()` adds the middleware that runs the resolution pipeline on each request and populates `ITenantContextAccessor.Current`.

The middleware:
1. Builds a `TenantResolutionRequest` with data from the `HttpContext` (host, headers, `ClaimsPrincipal`, `RequestServices`)
2. Deposits `TenantHttpRequestData` (host + headers) into `Items[TenantResolutionHttpKeys.HttpRequestData]`
3. Calls `ITenantResolverPipeline.ResolveAsync`
4. Calls `ITenantContextFactory.Create(result)` and `accessor.SetCurrent(ctx)`
5. Proceeds to the next middleware

### Recommended order in the pipeline

```csharp
app.UseAuthentication();          // populates ClaimsPrincipal
app.UseAuthorization();
app.UseTarsTenantResolution();    // tenant context already available for the rest
app.MapControllers();
```

If you do **not** use `ClaimTenantResolver`, the position relative to authentication does not matter. If you do, the tenant middleware must come after.

---

## Built-in HTTP resolvers

### `HeaderTenantResolver`

Reads the tenant key from an HTTP header. It does not inject `IHttpContextAccessor` — the header data arrives via `TenantResolutionRequest.Items`.

```csharp
builder.Services.AddTarsHeaderTenantResolver("X-Tenant-Key");
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<HeaderTenantResolver>();
});
```

Default header: `X-Tenant-Key`. Pass the desired name in the constructor or the helper.

Example request:

```http
GET /api/orders
Authorization: Bearer eyJ...
X-Tenant-Key: acme
```

### `SubdomainTenantResolver`

Extracts the first segment of the subdomain as the tenant key.

| Host | Tenant key |
|---|---|
| `acme.app.com` | `acme` |
| `globex.app.com` | `globex` |
| `app.com` | unresolved (no subdomain) |
| `localhost` | unresolved |

```csharp
builder.Services.AddTarsSubdomainTenantResolver();
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<SubdomainTenantResolver>();
});
```

---

## Common combinations

### Internal API — header only

```csharp
// Program.cs
builder.Services.AddTarsMultitenancy();
builder.Services.AddTarsHeaderTenantResolver("X-Tenant-Key");
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<HeaderTenantResolver>();
});

app.UseTarsTenantResolution();
```

### Public app — subdomain as primary, header as fallback (admin/tests)

```csharp
builder.Services.AddTarsMultitenancy();
builder.Services.AddTarsSubdomainTenantResolver();
builder.Services.AddTarsHeaderTenantResolver("X-Tenant-Key");
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver<SubdomainTenantResolver>();
    options.AddResolver<HeaderTenantResolver>();  // fallback
});

app.UseTarsTenantResolution();
```

### SaaS with login — JWT claim as primary

```csharp
builder.Services.AddTarsMultitenancy();
builder.Services.AddTarsHeaderTenantResolver("X-Tenant-Key");
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver(new ClaimTenantResolver("tenant_key")); // from the JWT
    options.AddResolver<HeaderTenantResolver>();                 // service calls
});

// Order matters: authentication before the tenant
app.UseAuthentication();
app.UseAuthorization();
app.UseTarsTenantResolution();
```

---

## Accessing the context in controllers

Inject `ITenantContext` or `ITenantContextAccessor` normally via DI:

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly IOrderService _orders;

    public OrdersController(ITenantContext tenantContext, IOrderService orders)
    {
        _tenantContext = tenantContext;
        _orders = orders;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!_tenantContext.IsResolved)
            return BadRequest("Tenant not identified.");

        var result = await _orders.GetAllAsync(_tenantContext.TenantKey!, ct);
        return Ok(result);
    }
}
```

Or via `ITenantContextAccessor` when you need to check and set the context:

```csharp
public class TenantMiddlewareExample
{
    private readonly ITenantContextAccessor _accessor;

    public TenantMiddlewareExample(ITenantContextAccessor accessor)
        => _accessor = accessor;

    public void LogCurrentTenant()
    {
        var ctx = _accessor.Current;
        if (ctx?.IsResolved == true)
            Console.WriteLine($"Tenant: {ctx.TenantKey}");
    }
}
```

---

## Minimal APIs

```csharp
app.MapGet("/api/products", async (
    ITenantContext tenantCtx,
    IProductRepository repo,
    CancellationToken ct) =>
{
    if (!tenantCtx.IsResolved)
        return Results.BadRequest("Tenant not identified.");

    var products = await repo.GetByTenantAsync(tenantCtx.TenantKey!, ct);
    return Results.Ok(products);
});
```

---

## Returning 401/400 when the tenant is not resolved

The framework does not block requests without a tenant automatically — that decision belongs to the application. Implement an action filter or your own middleware:

```csharp
public class RequireTenantFilter : IActionFilter
{
    private readonly ITenantContextAccessor _accessor;

    public RequireTenantFilter(ITenantContextAccessor accessor)
        => _accessor = accessor;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (_accessor.Current?.IsResolved != true)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Tenant not identified." });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
```

Global registration:

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequireTenantFilter>();
});
builder.Services.AddScoped<RequireTenantFilter>();
```

---

## `TenantHttpRequestData`

The object the middleware deposits into `Items[TenantResolutionHttpKeys.HttpRequestData]`. HTTP resolvers read from it instead of depending on `IHttpContextAccessor`.

```csharp
public sealed class TenantHttpRequestData
{
    public string? Host { get; }
    public IReadOnlyDictionary<string, string?> Headers { get; }
}
```

Custom resolvers that need HTTP request data should read from this object:

```csharp
if (!request.Items.TryGetValue(TenantResolutionHttpKeys.HttpRequestData, out var obj) ||
    obj is not TenantHttpRequestData data)
    return TenantResolutionResult.Unresolved();

var value = data.Headers.GetValueOrDefault("X-My-Header");
```
