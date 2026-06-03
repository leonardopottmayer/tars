# Web - Overview

## What is in this family

The `Web` family covers the framework's HTTP presentation layer:

- success and error envelopes (HTTP)
- mapping of `Result` and `Error` to HTTP
- automatic wrapping in controllers and Minimal APIs
- global exception filter
- writing of pagination headers

## HTTP

| Package | Level | Role |
|---|---|---|
| `Pottmayer.Tars.Web.Http.Abstractions` | Abstractions | framework-agnostic HTTP contracts |
| `Pottmayer.Tars.Web.Http` | Runtime | envelopes, `DefaultHttpErrorMapper`, `WrapDecisionService`, core options |
| `Pottmayer.Tars.Web.Http.AspNetCore` | Host Integration | MVC filters, endpoint filters, `TarsExceptionFilter`, ASP.NET Core extensions and options |

## Quick map

- [http.md](./http.md) - packages, DI, options, appsettings and complete scenarios
- [error-mapping.md](./error-mapping.md) - `IHttpErrorMapper`, messages, `TarsExceptionFilter`
- [response-wrapping.md](./response-wrapping.md) - wrapping in controllers and Minimal APIs
- [result-extensions.md](./result-extensions.md) - `ToActionResult()` (MVC), `ToHttpResult()` (Minimal API) and `WritePaginationHeaders()`
- [../core/localization.md](../core/localization.md) - the message base used by the default mapper

## Minimal registration

```csharp
builder.Services.AddTarsLocalization();
builder.AddTarsWebHttpOptions();
builder.Services.AddTarsDefaultHttpErrorMapper();
builder.Services.AddTarsDefaultWrapDecisionService();
```

For MVC with automatic wrapping:

```csharp
builder.AddTarsWebHttpAspNetCoreOptions();
builder.Services.AddTarsResponseWrapperResultFilter();
builder.Services.AddTarsResponseWrapperMvcOptionsSetup();
builder.Services.AddTarsExceptionFilter();

builder.Services.AddControllers(options =>
{
    options.Filters.AddService<TarsExceptionFilter>();
});
```

For Minimal APIs with per-group wrapping:

```csharp
builder.AddTarsWebHttpAspNetCoreOptions();
builder.Services.AddTarsResponseWrapperEndpointFilter();

var api = app.MapGroup("/api").AddTarsResponseWrapper();
```

## Quick example

```csharp
app.MapGet("/orders/{id:guid}", async (Guid id, IMediator mediator, IHttpErrorMapper mapper) =>
{
    var result = await mediator.SendAsync(new GetOrderQuery(id));
    return result.ToHttpResult(mapper);
});
```

Success response:

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "pending"
  }
}
```

Error response:

```json
{
  "success": false,
  "errorCode": "ORDER_NOT_FOUND",
  "errorMessage": "Resource not found."
}
```

## Important principles

- No `statusCode` in the body: the real status stays in the HTTP protocol.
- No hardcoded messages in the framework: the default mapper uses `IMessageProvider`.
- No DI aggregators: the `AddTars*` methods are granular.
- No forced coupling to MVC: `Web.Http` remains usable outside controllers.
- No universal wrapping in Minimal APIs: endpoints that return `IResult` stay under the control of the `IResult` itself.
