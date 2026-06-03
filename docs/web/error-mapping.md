# Error Mapping

## Role of `IHttpErrorMapper`

`IHttpErrorMapper` is the central contract that converts:

- `Error` and `ErrorType`
- `Exception`

into standardized HTTP responses.

Contract:

```csharp
public interface IHttpErrorMapper
{
    int MapToStatusCode(ErrorType errorType);
    IHttpErrorResponse Map(Error error);
    IHttpErrorResponse Map(Exception exception);
}
```

## Default mapping

`DefaultHttpErrorMapper` uses this map:

| `ErrorType` | HTTP status |
|---|---|
| `NotFound` | `404` |
| `Validation` | `422` |
| `Business` | `400` |
| `Conflict` | `409` |
| `Unauthorized` | `401` |
| `Forbidden` | `403` |
| others | `500` |

## How the message is chosen

For `Map(Error error)`:

1. if `error.Message` is filled in, it is used directly
2. otherwise, the mapper chooses the default key by `ErrorType`
3. that key is resolved by the `IMessageProvider`

Tars internal keys:

| Key | EN | PT-BR |
|---|---|---|
| `tars.http.not_found` | `Resource not found.` | `Recurso não encontrado.` |
| `tars.http.validation` | `One or more validation errors occurred.` | `Um ou mais erros de validação ocorreram.` |
| `tars.http.bad_request` | `Invalid request.` | `Requisição inválida.` |
| `tars.http.conflict` | `A conflict occurred.` | `Um conflito ocorreu.` |
| `tars.http.unauthorized` | `Authentication required.` | `Autenticação necessária.` |
| `tars.http.forbidden` | `Access denied.` | `Acesso negado.` |
| `tars.http.internal_server_error` | `An unexpected error occurred.` | `Ocorreu um erro inesperado.` |

These messages are added in memory by `AddTarsDefaultHttpErrorMapper()`.

## Generated response

Success does not go through `IHttpErrorMapper`. On failure, the default envelope is:

```json
{
  "success": false,
  "errorCode": "ORDER_NOT_FOUND",
  "errorMessage": "Resource not found."
}
```

For validation errors with metadata:

```json
{
  "success": false,
  "errorCode": "INVALID_ORDER",
  "errorMessage": "Validation failed.",
  "fieldErrors": [
    { "field": "quantity", "message": "Quantity must be greater than zero." },
    { "field": "customerId", "message": "Customer not found." }
  ]
}
```

## `FieldErrors`

`DefaultHttpErrorMapper` builds `FieldErrors` only when:

- `error.Type == ErrorType.Validation`
- `error.Metadata` is not `null`

Each pair `Metadata[key] = value` becomes:

```json
{ "field": "key", "message": "value" }
```

Example of producing the error:

```csharp
return Result<OrderDto>.Failure(
    new Error(
        "INVALID_ORDER",
        "Validation failed.",
        ErrorType.Validation,
        new Dictionary<string, object?>
        {
            ["quantity"] = "Quantity must be greater than zero.",
            ["customerId"] = "Customer not found."
        }));
```

## `TarsExceptionFilter`

The MVC exception filter uses the mapper in two paths.

### Expected exception

If the exception implements `IExpectedException`:

- takes the first `Error`
- computes the status via `MapToStatusCode(first.Type)`
- builds the body with `Map(first)`

Example:

```csharp
public sealed class OrderNotFoundException : Exception, IExpectedException
{
    public IReadOnlyList<Error> Errors { get; } =
    [
        new Error("ORDER_NOT_FOUND", "", ErrorType.NotFound)
    ];
}
```

Response:

```json
{
  "success": false,
  "errorCode": "ORDER_NOT_FOUND",
  "errorMessage": "Resource not found."
}
```

If `Errors` is empty, the filter falls back to `Map(exception)` with status `400`.

### Unexpected exception

For ordinary exceptions:

- status `500`
- `errorCode = "INTERNAL_SERVER_ERROR"`
- message localized by the key `tars.http.internal_server_error`

## `ToHttpResult()` and 401/403

`ResultHttpExtensions.ToHttpResult()` uses the mapper to find the status, but it does not return an envelope in all cases:

- `401` becomes `Results.Unauthorized()`
- `403` becomes `Results.Forbid()`

That is, in these two cases the HTTP protocol speaks for itself and the body is not built by the helper.

## Custom mapper

An application can replace the mapper entirely:

```csharp
public sealed class AppHttpErrorMapper : IHttpErrorMapper
{
    private readonly IMessageProvider _messages;

    public AppHttpErrorMapper(IMessageProvider messages)
        => _messages = messages;

    public int MapToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.NotFound     => 404,
        ErrorType.Validation   => 400,
        ErrorType.Business     => 422,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden    => 403,
        ErrorType.Conflict     => 409,
        _                      => 500
    };

    public IHttpErrorResponse Map(Error error)
        => new HttpErrorResponse
        {
            Success = false,
            ErrorCode = error.Code,
            ErrorMessage = _messages.Get(
                $"app.errors.{error.Code.ToLowerInvariant()}",
                fallback: error.Message),
            FieldErrors = error.Metadata?
                .Select(kv => (IHttpFieldError)new HttpFieldError(
                    kv.Key,
                    kv.Value?.ToString() ?? string.Empty))
                .ToList()
        };

    public IHttpErrorResponse Map(Exception exception)
        => new HttpErrorResponse
        {
            Success = false,
            ErrorCode = "INTERNAL_ERROR",
            ErrorMessage = _messages.Get(
                "app.errors.internal",
                fallback: "An unexpected error occurred.")
        };
}

builder.Services.AddTarsLocalization();
builder.Services.AddTarsHttpErrorMapper<AppHttpErrorMapper>();
```

## Overriding only the Tars messages

If the default mapper is still good, but the application wants to rewrite the default texts:

```csharp
builder.Services.AddTarsLocalization();

builder.Services.AddTarsMessageSource(new InMemoryMessageSource(
    new Dictionary<string, IDictionary<string, string>>
    {
        ["pt-BR"] = new Dictionary<string, string>
        {
            ["tars.http.not_found"] = "Registro não localizado.",
            ["tars.http.validation"] = "Existem erros de validação na requisição."
        }
    }));

builder.Services.AddTarsDefaultHttpErrorMapper();
```

Since sources are queried in registration order, the override can come before other application sources.

## Practical recommendation

- Use the default mapper when `ErrorType` is enough for your HTTP protocol.
- Use a custom mapper when the error code needs to become a localization key or when your business requires a different status map.
- Avoid filling `Error.Message` with fixed text if you expect automatic localization by the default mapper.
