# Result Extensions and Pagination

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
