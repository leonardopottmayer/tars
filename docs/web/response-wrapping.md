# Response Wrapping

## What wrapping does

Automatic wrapping turns ordinary responses into `HttpResponse<T>` or `HttpErrorResponse` envelopes, without requiring each endpoint to build those objects manually.

There are two paths:

- `ResponseWrapperResultFilter` for MVC/controllers
- `ResponseWrapperEndpointFilter` for Minimal APIs

The final behavior depends on:

- `WebHttpOptions.Enabled`
- `WebHttpOptions.IncludeTraceId`
- `WebHttpAspNetCoreOptions.ControllersDefaultMode`
- `WebHttpAspNetCoreOptions.MinimalApisEnabledByDefault`
- explicit enable/disable metadata/attributes

## Core rule

`WrapDecisionService` uses this order:

1. if wrapping is off, do not wrap
2. if the response is already wrapped, do not wrap again
3. if it is a file/stream, do not wrap
4. if there is an explicit disable, do not wrap
5. if there is a Minimal API opt-in, wrap
6. if there is an explicit enable in MVC, wrap
7. otherwise, follow `ControllersDefaultMode`

## Controllers

### How the filter works

`ResponseWrapperResultFilter`:

- ignores `FileResult`, redirects, `ChallengeResult`, `ForbidResult`, `UnauthorizedResult` and the like
- if it receives `EmptyResult` or `NoContentResult`, it can produce an `HttpResponse<object?>` with `Data = null`
- if it receives `ObjectResult` with an ordinary payload, it wraps it in `HttpResponse<object?>`
- if the HTTP status is `>= 400`, it tries to produce an `HttpErrorResponse`

### Error cases in MVC

For an error `ObjectResult`:

- `ValidationProblemDetails` becomes `HttpErrorResponse` with `FieldErrors`
- `ProblemDetails` becomes `HttpErrorResponse` with `ErrorCode` coming from `Extensions["code"]` or `Title`
- other objects become `HttpErrorResponse` with `ErrorCode = "ERROR"` and `ErrorMessage = value?.ToString()`

Important:

- the MVC filter does not use `IHttpErrorMapper` for every error `ObjectResult`
- the mapper comes into play in the exception flow (`TarsExceptionFilter`) and in endpoints that use `ToHttpResult()`

### Controller example

```csharp
[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id)
        => Ok(new { id, status = "pending" });
}
```

Response:

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "pending"
  }
}
```

### `WrapAll` and `WrapNone`

`WrapAll`:

```json
{
  "Tars": {
    "Web": {
      "Http": {
        "ControllersDefaultMode": "WrapAll"
      }
    }
  }
}
```

`WrapNone`:

```json
{
  "Tars": {
    "Web": {
      "Http": {
        "ControllersDefaultMode": "WrapNone"
      }
    }
  }
}
```

### `[DisableResponseWrapper]`

```csharp
[ApiController]
[Route("internal")]
[DisableResponseWrapper]
public sealed class InternalController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(new { ok = true });
}
```

### `[EnableResponseWrapper]`

Useful when the default mode is `WrapNone`:

```csharp
[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EnableResponseWrapper]
    public IActionResult Get(Guid id)
        => Ok(new { id, status = "pending" });
}
```

## Minimal APIs

### How the filter works

`ResponseWrapperEndpointFilter` runs after the endpoint delegate and only wraps when:

- wrapping is on
- the return is not `null`
- the return is not `IHttpResponse`
- the return is not `IResult`
- the metadata/configuration rule says to wrap

This detail is the most important of the module:

- an endpoint that returns a POCO can be wrapped
- an endpoint that returns `Results.Ok(...)`, `Results.File(...)`, `Results.Problem(...)` or any other `IResult` does not get additional wrapping

### Per-group opt-in

```csharp
builder.Services.AddTarsResponseWrapperEndpointFilter();

var api = app.MapGroup("/api").AddTarsResponseWrapper();

api.MapGet("/orders", () => new[]
{
    new { id = Guid.NewGuid(), status = "pending" }
});
```

Response:

```json
{
  "success": true,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "status": "pending"
    }
  ]
}
```

### Per-endpoint opt-out

```csharp
api.MapGet("/health", () => new { ok = true })
   .DisableTarsResponseWrapper();
```

### On by default

```json
{
  "Tars": {
    "Web": {
      "Http": {
        "MinimalApisEnabledByDefault": true
      }
    }
  }
}
```

Even so, `IResult` remains without extra wrapping.

## `ToHttpResult()` versus endpoint wrapping

If you do this:

```csharp
api.MapGet("/orders/{id:guid}", async (Guid id, IMediator mediator, IHttpErrorMapper mapper) =>
{
    var result = await mediator.SendAsync(new GetOrderQuery(id));
    return result.ToHttpResult(mapper);
});
```

the endpoint returns `IResult`, so the `ResponseWrapperEndpointFilter` does not add a second envelope.

In practice, choose one of the two styles per endpoint:

- return a POCO and let the filter wrap it
- return `IResult` explicitly with `ToHttpResult()`

## `traceId`

When `IncludeTraceId = true`, the automatic filters add:

```json
{
  "success": true,
  "data": { },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

Important:

- `traceId` is added by the filters
- `ToHttpResult()` does not consult `WebHttpOptions`, so it does not inject `traceId`

## Responses skipped on purpose

The filters let through without wrapping:

- files
- redirects
- `ChallengeResult`
- `ForbidResult`
- `UnauthorizedResult`
- `IResult` in Minimal APIs
- objects already implementing `IHttpResponse`

These cases preserve the native ASP.NET Core behavior.

## Practical recommendation

- Use wrapping in controllers when the team wants an automatic standard.
- In Minimal APIs, prefer per-group wrapping only for endpoints that return simple objects.
- For endpoints that need fine-grained control of status/body, use `ToHttpResult()` or `Results.*()` directly.
