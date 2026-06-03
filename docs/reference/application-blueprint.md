# Application Blueprint

## Goal

This guide shows how to compose an entire application on top of `Pottmayer.Tars`, combining the modules most used day to day.

## Typical minimal stack

For an HTTP backend with authentication, data access, localized messages and standardized responses, the most common set is:

- `Core.Primitives`
- `Core.Mediator.Abstractions`
- `Core.Mediator`
- `Core.Cqrs`
- `Core.Localization.Abstractions`
- `Core.Localization`
- `Core.Localization.AspNetCore`
- `Data.Relational.Abstractions`
- `Data.Relational`
- `Web.Http`
- `Web.Http.AspNetCore`
- `Security.Identity`
- `Security.Identity.AspNetCore`
- `UserContext`
- `UserContext.AspNetCore`
- optionally `Caching.*`
- optionally `Multitenancy.*`

## Recommended composition order

### 1. Data

Register the data infrastructure first, because several modules depend on it.
Each method registers a single service — this allows replacing any component individually.

```csharp
builder.Services.AddTarsDataContextAccessor();
builder.Services.AddTarsRelationalCompositeConnectionResolver();
builder.Services.AddTarsRelationalConfigurationConnectionResolver();
builder.Services.AddTarsDataContextFactory();
builder.Services.AddTarsRelationalUnitOfWorkFactory();

builder.Services.AddTarsData<AppDbContext>((sp, descriptor) =>
    new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(descriptor.ConnectionString)
        .Options);

builder.Services.AddTarsDataRepositoriesFromAssemblies(typeof(AppAssemblyMarker).Assembly);
```

### 2. Core application

Register the mediator, handlers and behaviors:

```csharp
builder.Services.AddTarsMediator(options =>
{
    options.RegisterHandlersFromAssembly(typeof(MyCommand).Assembly);
});
builder.Services.AddTarsCqrsExceptionMappingBehavior();
```

### 3. Localization

Localize framework and application messages:

```csharp
builder.AddTarsLocalizationAspNetCore();
builder.Services.AddTarsStringLocalizerSource<MyApp.Resources.SharedResource>();
```

### 4. User context

Register the typed user resolution:

```csharp
builder.AddTarsUserContextOptions();
builder.Services.AddTarsClaimsUserResolver<UserData>();
builder.Services.AddTarsDefaultUserContextFactory<UserData>();
builder.Services.AddTarsUserContextAccessor<UserData>();
builder.Services.AddTarsCurrentPrincipalAccessor();
```

### 5. Identity

Register the core and web options, the issuance/validation services and the transport:

```csharp
builder.AddTarsIdentityOptions();
builder.AddTarsIdentityAspNetCoreOptions();

builder.Services.AddTarsIdentityJwtTokenIssuer();
builder.Services.AddTarsIdentityJwtTokenValidator();
builder.Services.AddTarsIdentityRefreshTokenService();
builder.Services.AddTarsIdentityAspNetCoreJwtBearer();
```

### 6. Web

Configure HTTP, wrapping and exceptions:

```csharp
builder.AddTarsWebHttpOptions();
builder.AddTarsWebHttpAspNetCoreOptions();

builder.Services.AddTarsDefaultHttpErrorMapper();
builder.Services.AddTarsDefaultWrapDecisionService();
builder.Services.AddTarsResponseWrapperResultFilter();
builder.Services.AddTarsResponseWrapperMvcOptionsSetup();
builder.Services.AddTarsResponseWrapperEndpointFilter();
builder.Services.AddTarsExceptionFilter();

builder.Services.AddControllers(options =>
{
    options.Filters.AddService<TarsExceptionFilter>();
});
```

### 7. Caching

```csharp
builder.AddTarsCachingOptions();
builder.Services.AddTarsCacheKeyBuilder();
builder.Services.AddTarsCacheSerializer();
builder.Services.AddMemoryCache();
builder.Services.AddTarsMemoryCacheProvider();
```

### 8. Multitenancy

If the application is multi-tenant:

```csharp
builder.Services.AddTarsMultitenancy();
builder.Services.AddTarsTenantResolution(options =>
{
    options.AddResolver(new HeaderTenantResolver("X-Tenant-Key"));
});
builder.Services.AddTarsTenantCatalog<ConfigurationTenantCatalog>();
```

In the pipeline:

```csharp
app.UseTarsTenantResolution();
```

## Example `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddTarsLocalizationAspNetCore();
builder.AddTarsWebHttpOptions();
builder.AddTarsWebHttpAspNetCoreOptions();

builder.Services.AddTarsStringLocalizerSource<MyApp.Resources.SharedResource>();
builder.Services.AddTarsDefaultHttpErrorMapper();
builder.Services.AddTarsDefaultWrapDecisionService();
builder.Services.AddTarsResponseWrapperResultFilter();
builder.Services.AddTarsResponseWrapperMvcOptionsSetup();
builder.Services.AddTarsResponseWrapperEndpointFilter();
builder.Services.AddTarsExceptionFilter();

builder.Services.AddControllers(options =>
{
    options.Filters.AddService<TarsExceptionFilter>();
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseTarsLocalization();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var api = app.MapGroup("/api");
api.MapGet("/orders/{id:guid}", async (Guid id, IMediator mediator, IHttpErrorMapper mapper) =>
{
    var result = await mediator.SendAsync(new GetOrderQuery(id));
    return result.ToHttpResult(mapper);
});

app.Run();
```

## Recommended organization strategy

The `Pandora` and `Roberto` examples follow an organization that fits the framework well:

- `Adapter.Data`: composes pipelines, repositories, EF interceptors, the domain dispatcher
- `Adapter.Identity`: composes identity core + AspNetCore + concrete authenticators
- `Adapter.Web`: composes wrapping, the exception filter and HTTP concerns
- `Adapter.UserContext`: composes user resolution and the fallback user
- `Core.Application`: commands, queries, handlers, behaviors
- `Core.Domain`: entities, aggregates, domain repositories
- `Host`: final ASP.NET Core wiring

## Where to customize

- messages: `IMessageSource` or `IMessageProvider`
- concrete authentication: identity stores, authenticators and handlers
- multitenancy: `ITenantCatalog` and/or `ITenantResolver`
- connection resolution: `IDataConnectionResolver`
- HTTP response model: `IHttpErrorMapper`, `IWrapDecisionService`, `WebHttpOptions`, `WebHttpAspNetCoreOptions`
- user typing: model your `UserData` class and use `ClaimAttribute` when needed
