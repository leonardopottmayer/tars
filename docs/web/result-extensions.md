# Result Extensions and Pagination

`ResultHttpExtensions` converts `Result` / `Result<T>` into an HTTP response. Two helpers:

- `ToActionResult()` → `IActionResult`, for **MVC controllers**.
- `ToHttpResult()` → `IResult`, for **Minimal APIs**.

They share the same envelope shape but differ in two intentional ways — see [ToActionResult vs ToHttpResult](#toactionresult-vs-tohttpresult).

## `ToActionResult()`

For MVC controllers. Both overloads resolve a `traceId` from `Activity.Current` and
include it in every response — success and failure alike.

Signatures:

```csharp
public static IActionResult ToActionResult<T>(this Result<T> result, IHttpErrorMapper mapper)
    where T : notnull;

public static IActionResult ToActionResult(this Result result, IHttpErrorMapper mapper);
```

### Success mapping

`Result<T>.Success(value)` produces an `OkObjectResult` wrapping `HttpResponse<T>`:

```json
{
  "success": true,
  "data": { "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "status": "pending" },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

`Result.Success()` produces `{ "success": true, "data": null, "traceId": "..." }`.

### Failure mapping

The first error is taken from `result.Errors`, the status comes from
`mapper.MapToStatusCode(error.Type)`, and the body is the `HttpErrorResponse`
envelope returned by `mapper.Map(error)` — always as an `ObjectResult` carrying
that body, **including `401` and `403`**:

```json
{
  "success": false,
  "errorCode": "ORDER_NOT_FOUND",
  "errorMessage": "Resource not found.",
  "fieldErrors": null,
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

If `result.Errors` is empty on a failure, the helper returns a `500` envelope with
`errorCode = "ERROR"`.

### Controller example

```csharp
[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(ISender sender, IHttpErrorMapper errorMapper) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetOrderQuery(id), ct);
        return result.ToActionResult(errorMapper);
    }
}
```

> Note: `ToActionResult()` builds the envelope itself, so the response is already
> wrapped. The automatic `ResponseWrapperResultFilter` detects this (`IHttpResponse`)
> and does not wrap it a second time. For middleware-level auth failures (`401`/`403`
> raised before the action runs) the controller is never reached — see
> [error-mapping.md](./error-mapping.md#auth-challengeforbidden-responses-401403).

## `ToActionResult` vs `ToHttpResult`

| Behavior | `ToActionResult` (MVC) | `ToHttpResult` (Minimal API) |
|---|---|---|
| `traceId` in the body | injected from `Activity.Current` | not injected |
| `401` / `403` | carry the Tars envelope (`ObjectResult`) | `Results.Unauthorized()` / `Results.Forbid()`, no envelope |
| Empty errors on failure | `500` envelope (`errorCode = "ERROR"`) | `Results.Problem(statusCode: 500)` |

## `ToHttpResult()`

`ResultHttpExtensions` converts `Result` and `Result<T>` into `IResult` for Minimal APIs.

Signatures:

```csharp
public static IResult ToHttpResult<T>(this Result<T> result, IHttpErrorMapper mapper)
    where T : notnull;

public static IResult ToHttpResult(this Result result, IHttpErrorMapper mapper);
```

## Success mapping

For `Result<T>.Success(value)`:

```csharp
return Results.Ok(new HttpResponse<T>
{
    Success = true,
    Data = value
});
```

Example:

```csharp
app.MapGet("/orders/{id:guid}", async (Guid id, IMediator mediator, IHttpErrorMapper mapper) =>
{
    var result = await mediator.SendAsync(new GetOrderQuery(id));
    return result.ToHttpResult(mapper);
});
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

For `Result.Success()`:

```json
{
  "success": true,
  "data": null
}
```

## Failure mapping

`ToHttpResult()` takes the first error from `result.Errors`.

| Status | Output |
|---|---|
| `400` | `Results.BadRequest(response)` |
| `401` | `Results.Unauthorized()` |
| `403` | `Results.Forbid()` |
| `404` | `Results.NotFound(response)` |
| `409` | `Results.Conflict(response)` |
| `422` | `Results.UnprocessableEntity(response)` |
| others | `Results.Problem(first.Message, statusCode: statusCode)` |

Example:

```csharp
app.MapPost("/orders", async (CreateOrderRequest request, IMediator mediator, IHttpErrorMapper mapper) =>
{
    var result = await mediator.SendAsync(new CreateOrderCommand(request));
    return result.ToHttpResult(mapper);
});
```

## Important behaviors

- if `result.Errors` is empty on a failure, the helper returns `Results.Problem(statusCode: 500)`
- `ToHttpResult()` does not inject a `traceId`
- `ToHttpResult()` does not write pagination headers
- `401` and `403` do not carry the Tars envelope

> These last two points are where `ToHttpResult` differs from the MVC `ToActionResult`,
> which injects `traceId` and wraps `401`/`403` in the envelope. See
> [ToActionResult vs ToHttpResult](#toactionresult-vs-tohttpresult).

## HTTP pagination

`PaginationExtensions.WritePaginationHeaders()` writes:

- `X-Pagination-Page`
- `X-Pagination-PageSize`
- `X-Pagination-TotalCount`
- `X-Pagination-TotalPages`

The method expects any object that implements `IPaginationInfo`.

### Paginated DTO example

```csharp
public sealed record OrdersPageDto(
    IReadOnlyList<OrderDto> Items,
    int Page,
    int PageSize,
    long TotalCount,
    int TotalPages) : IPaginationInfo;
```

### Minimal API example

```csharp
app.MapGet("/orders", async (HttpContext http, IMediator mediator) =>
{
    var page = await mediator.SendAsync(new ListOrdersQuery(page: 1, pageSize: 20));
    http.Response.WritePaginationHeaders(page);
    return page.Items;
})
.AddTarsResponseWrapper();
```

With this format:

- the items go in the endpoint filter's normal envelope
- the metadata goes in the headers

### Controller example

```csharp
[HttpGet]
public async Task<IActionResult> GetOrders()
{
    var page = await _mediator.SendAsync(new ListOrdersQuery(page: 1, pageSize: 20));
    Response.WritePaginationHeaders(page);
    return Ok(page.Items);
}
```

## When to use each strategy

- Use `ToHttpResult()` when the endpoint already works naturally with `Result`/`Result<T>`.
- Use `WritePaginationHeaders()` when the page metadata needs to go into headers, regardless of the body.
- Combine the two only when it makes sense for the endpoint; they solve different concerns.
