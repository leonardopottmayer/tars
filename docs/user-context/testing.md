# User Context — Testing

Both User Context systems were designed to be testable without complex mocks.

---

## Testing services that use the Claims system (`IUserContext`)

### Injecting an authenticated context

```csharp
public class AuditServiceTests
{
    [Fact]
    public async Task RecordAsync_WhenAuthenticated_SavesUserId()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-abc"),
            new(ClaimTypes.Email,          "user@example.com"),
            new(ClaimTypes.Role,           "editor"),
        };

        var accessor = new AsyncLocalUserContextAccessor();
        accessor.Current = new UserContext(claims);

        var repo    = Substitute.For<IAuditRepository>();
        var service = new AuditService(accessor.Current, repo);

        // Act
        await service.RecordAsync("edit-document");

        // Assert
        await repo.Received(1).InsertAsync(Arg.Is<AuditEntry>(e => e.UserId == "user-abc"));
    }

    [Fact]
    public async Task RecordAsync_WhenAnonymous_DoesNotSave()
    {
        var service = new AuditService(UserContext.Anonymous, Substitute.For<IAuditRepository>());

        await service.RecordAsync("view-page");

        await Substitute.For<IAuditRepository>().DidNotReceiveWithAnyArgs().InsertAsync(default!);
    }
}
```

### Using the accessor with DI

```csharp
public class OrderServiceTests
{
    private static IServiceProvider BuildServices(IUserContext? userContext = null)
    {
        var services = new ServiceCollection();

        services.AddTarsUserContextAccessor();
        services.AddTarsUserContext();
        services.AddScoped<OrderService>();
        // ... other registrations

        var sp = services.BuildServiceProvider();

        if (userContext is not null)
            sp.GetRequiredService<IUserContextAccessor>().Current = userContext;

        return sp;
    }

    [Fact]
    public async Task PlaceOrderAsync_WithAuthenticatedUser_CreatesOrder()
    {
        var sp = BuildServices(new UserContext([
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
        ]));

        var service = sp.GetRequiredService<OrderService>();
        var order   = await service.PlaceOrderAsync(new OrderRequest { Items = ["item-1"] });

        Assert.Equal("user-123", order.CustomerId);
    }

    [Fact]
    public async Task PlaceOrderAsync_WithAnonymousUser_Throws()
    {
        var sp = BuildServices();   // no context → returns Anonymous

        var service = sp.GetRequiredService<OrderService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.PlaceOrderAsync(new OrderRequest()));
    }
}
```

---

## Testing services that use the Typed system (`IUserContext<TUser>`)

### Creating a typed context directly

```csharp
public class TenantOrderServiceTests
{
    [Fact]
    public async Task GetOrdersAsync_ReturnsOnlyTenantOrders()
    {
        // UserContext<TUser> can be instantiated directly
        var user = new AppUser
        {
            Id       = Guid.NewGuid(),
            TenantId = "tenant-acme",
            Name     = "Alice",
        };

        var userContext = new UserContext<AppUser>(isAuthenticated: true, user: user);

        var accessor = Substitute.For<IUserContextAccessor<AppUser>>();
        accessor.Context.Returns(userContext);

        var repo    = Substitute.For<IOrderRepository>();
        var service = new TenantOrderService(accessor, repo);

        await service.GetOrdersAsync();

        await repo.Received(1).GetByTenantAsync("tenant-acme");
    }

    [Fact]
    public async Task GetOrdersAsync_WhenNotAuthenticated_Throws()
    {
        var userContext = new UserContext<AppUser>(isAuthenticated: false, user: null);

        var accessor = Substitute.For<IUserContextAccessor<AppUser>>();
        accessor.Context.Returns(userContext);

        var service = new TenantOrderService(accessor, Substitute.For<IOrderRepository>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(service.GetOrdersAsync);
    }
}
```

### With full DI and a fake `ICurrentPrincipalAccessor`

```csharp
// PrincipalAccessorFake.cs — reusable across tests
public sealed class PrincipalAccessorFake : ICurrentPrincipalAccessor
{
    private readonly ClaimsPrincipal _principal;

    public PrincipalAccessorFake(ClaimsPrincipal principal) => _principal = principal;

    public static PrincipalAccessorFake Authenticated(params Claim[] claims)
    {
        var identity  = new ClaimsIdentity(claims, authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        return new PrincipalAccessorFake(principal);
    }

    public static PrincipalAccessorFake Anonymous()
        => new(new ClaimsPrincipal());

    public ClaimsPrincipal? Principal => _principal;
}

// In the test
public class ReportServiceTests
{
    [Fact]
    public async Task Generate_WithValidUser_ReturnsOwnerReport()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ICurrentPrincipalAccessor>(
            PrincipalAccessorFake.Authenticated(
                new Claim(ClaimTypes.NameIdentifier, "user-xyz"),
                new Claim(ClaimTypes.Name, "Bob"),
                new Claim("tenant_id", "tenant-beta")));

        services.AddOptions<UserContextOptions>()
            .Configure(o =>
            {
                o.ThrowOnConversionError       = true;
                o.ThrowOnMissingRequiredUserId = true;
                o.UseFallbackUserWhenAnonymous = false;
            });

        services.AddTarsClaimsUserResolver<AppUser>();
        services.AddTarsDefaultUserContextFactory<AppUser>();
        services.AddTarsUserContextAccessor<AppUser>();
        services.AddScoped<ReportService>();

        var sp = services.BuildServiceProvider();

        var service = sp.GetRequiredService<ReportService>();
        var report  = await service.GenerateAsync();

        Assert.Equal("Bob", report.Owner);
    }
}
```

---

## Testing the middleware

```csharp
public class UserContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenAuthenticated_SetsAccessorCurrent()
    {
        // Arrange
        var accessor    = new AsyncLocalUserContextAccessor();
        var capturedCtx = (IUserContext?)null;

        var middleware = new UserContextMiddleware(httpCtx =>
        {
            capturedCtx = accessor.Current;
            return Task.CompletedTask;
        });

        var claims   = new[] { new Claim(ClaimTypes.NameIdentifier, "u-1") };
        var identity = new ClaimsIdentity(claims, authenticationType: "bearer");
        var httpCtx  = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        // Act
        await middleware.InvokeAsync(httpCtx, accessor);

        // Assert — inside the pipeline the context was populated
        Assert.NotNull(capturedCtx);
        Assert.True(capturedCtx!.IsAuthenticated);
        Assert.Equal("u-1", capturedCtx.UserId);

        // After the pipeline the context was cleared
        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task InvokeAsync_WhenAnonymous_DoesNotSetAccessor()
    {
        var accessor = new AsyncLocalUserContextAccessor();
        IUserContext? capturedCtx = null;

        var middleware = new UserContextMiddleware(ctx =>
        {
            capturedCtx = accessor.Current;
            return Task.CompletedTask;
        });

        var httpCtx = new DefaultHttpContext { User = new ClaimsPrincipal() };  // anonymous

        await middleware.InvokeAsync(httpCtx, accessor);

        Assert.Null(capturedCtx);
        Assert.Null(accessor.Current);
    }
}
```

---

## Recommended reusable helpers

### `UserContextBuilder` for tests

```csharp
public static class UserContextBuilder
{
    public static IUserContext Authenticated(
        string userId  = "test-user",
        string? name   = "Test User",
        string? email  = "test@example.com",
        string? role   = null,
        params (string type, string value)[] extra)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
        };

        if (name  is not null) claims.Add(new(ClaimTypes.Name,  name));
        if (email is not null) claims.Add(new(ClaimTypes.Email, email));
        if (role  is not null) claims.Add(new(ClaimTypes.Role,  role));

        foreach (var (type, value) in extra)
            claims.Add(new(type, value));

        return new UserContext(claims);
    }

    public static IUserContext Anonymous() => UserContext.Anonymous;
}

// Usage in tests
accessor.Current = UserContextBuilder.Authenticated(
    userId: "admin-1",
    role:   "admin",
    extra:  [("tenant_id", "tenant-xyz")]);
```

### `AppUserBuilder` for the Typed system

```csharp
public static class AppUserBuilder
{
    public static AppUser Default(
        Guid? id       = null,
        string? name   = "Test User",
        string? email  = "test@example.com",
        string? tenant = "tenant-test")
        => new()
        {
            Id       = id ?? Guid.NewGuid(),
            Name     = name,
            Email    = email,
            TenantId = tenant,
        };

    public static IUserContext<AppUser> AsContext(AppUser? user = null, bool authenticated = true)
        => new UserContext<AppUser>(authenticated, user ?? Default());

    public static IUserContextAccessor<AppUser> AsAccessor(AppUser? user = null)
    {
        var accessor = Substitute.For<IUserContextAccessor<AppUser>>();
        accessor.Context.Returns(AsContext(user));
        return accessor;
    }
}

// Usage in tests
var service = new TenantOrderService(AppUserBuilder.AsAccessor(), repo);
```

---

## Integration with WebApplicationFactory

For integration tests with simulated authentication:

```csharp
public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    public string AuthenticatedUserId { get; set; } = "integration-user";
    public string AuthenticatedTenantId { get; set; } = "tenant-test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace ICurrentPrincipalAccessor with the fake
            services.RemoveAll<ICurrentPrincipalAccessor>();
            services.AddSingleton<ICurrentPrincipalAccessor>(_ =>
                PrincipalAccessorFake.Authenticated(
                    new Claim(ClaimTypes.NameIdentifier, AuthenticatedUserId),
                    new Claim("tenant_id",               AuthenticatedTenantId)));

            // Replace IUserContextAccessor with a pre-populated one (Claims system)
            services.RemoveAll<IUserContextAccessor>();
            var accessor = new AsyncLocalUserContextAccessor();
            accessor.Current = UserContextBuilder.Authenticated(
                userId: AuthenticatedUserId,
                extra:  [("tenant_id", AuthenticatedTenantId)]);
            services.AddSingleton<IUserContextAccessor>(accessor);
        });
    }
}

// Usage
public class OrdersApiTests(CustomWebAppFactory factory) : IClassFixture<CustomWebAppFactory>
{
    [Fact]
    public async Task GetOrders_ReturnsOnlyUserOrders()
    {
        factory.AuthenticatedUserId   = "user-999";
        factory.AuthenticatedTenantId = "tenant-acme";

        var client   = factory.CreateClient();
        var response = await client.GetAsync("/orders");

        response.EnsureSuccessStatusCode();
    }
}
```
