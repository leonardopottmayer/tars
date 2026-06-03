# Security / Identity — Token Delivery

The delivery mode controls where tokens are written in the response: HTTP header, cookie or JSON body.

---

## Available modes

| Mode | Access Token | Refresh Token | Typical use |
|---|---|---|---|
| `HeaderOnly` | `Authorization: Bearer <token>` | `X-Refresh-Token: <token>` | REST APIs (mobile, CLI, server) |
| `CookieOnly` | Cookie `tars.at` (HttpOnly) | Cookie `tars.rt` (HttpOnly) | Browser SPA without complex CORS |
| `BodyOnly` | `access_token` field in the JSON | `refresh_token` field in the JSON | When the client needs to store them explicitly |
| `Hybrid` | Decided by `X-Client-Type` or a heuristic | Same | When the same API serves both browser and mobile app |

---

## Configuration

```json
// appsettings.json
{
  "Tars": {
    "Security": {
      "Identity": {
        "TokenDelivery": {
          "Mode": "Hybrid"
        },
        "TokenDelivery": {
          "HybridClientTypeHeader": "X-Client-Type",
          "HybridCookieClientTypeValue": "web",
          "HybridHeaderClientTypeValue": "api"
        }
      }
    }
  }
}
```

```csharp
// Registration
builder.Services.AddTarsIdentityAspNetCoreTokenTransport();
// Includes: HeaderTokenReader, CookieTokenReader, CompositeTokenReader (as ITokenInputReader),
//           TokenOutputWriter (as ITokenOutputWriter)
```

---

## HeaderOnly

Ideal for APIs consumed by non-browser clients:

```json
"TokenDelivery": {
  "Mode": "HeaderOnly"
}
```

The client receives:
```
HTTP/1.1 200 OK
Authorization: Bearer eyJhbGc...
X-Refresh-Token: abc:xyz
Cache-Control: no-store
```

The client must send on the next requests:
```
Authorization: Bearer eyJhbGc...
```

---

## CookieOnly

Ideal for SPAs where cookies are managed automatically by the browser:

```json
"TokenDelivery": {
  "Mode": "CookieOnly"
},
"Cookie": {
  "AccessTokenCookieName": "app.at",
  "RefreshTokenCookieName": "app.rt",
  "Path": "/",
  "SameSite": 1,
  "HttpOnly": true,
  "SecurePolicy": true
}
```

The `app.at` cookie is read automatically by the `JwtBearer` middleware via `ITokenInputReader`.

**Caution with CORS:** if the SPA is on a different domain from the API, configure:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy => policy
        .WithOrigins("https://my-spa.com")
        .AllowCredentials()   // required for cookies
        .AllowAnyHeader()
        .AllowAnyMethod());
});
app.UseCors("spa");
```

---

## BodyOnly

The response body includes the tokens directly:

```json
{
  "access_token": "eyJhbGc...",
  "refresh_token": "abc:xyz",
  "expires_at": 1748649600,
  "token_type": "Bearer"
}
```

The client stores them in `localStorage` / `sessionStorage` and sends them as `Authorization: Bearer`.

---

## Hybrid

Hybrid mode lets the same API serve the browser (cookies) and API clients (headers), deciding based on the `X-Client-Type` header:

```http
# Browser SPA → sends X-Client-Type: web → receives cookies
POST /identity/sign-in/password
X-Client-Type: web
→ response: Set-Cookie: app.at=...; app.rt=...

# Mobile app → sends X-Client-Type: api → receives headers
POST /identity/sign-in/password
X-Client-Type: api
→ response: Authorization: Bearer ..., X-Refresh-Token: ...
```

Heuristic when `X-Client-Type` is not sent:
- If the request has `Authorization: Bearer` → deliver via header
- If the request has an auth cookie → deliver via cookie
- Default → header

---

## Reading tokens on subsequent requests

The `CompositeTokenReader` is registered as `ITokenInputReader` and is used by the JwtBearer middleware automatically. It combines `HeaderTokenReader` and `CookieTokenReader` based on the configured mode.

`JwtBearer` is configured via `ConfigureJwtBearerFromIdentityOptions`, which reads the Tars options:

```csharp
builder.Services.AddAuthentication()
    .AddTarsIdentityJwtBearer();
```

This configures JwtBearer to:
- Read the token from the header or cookie via `ITokenInputReader`
- Use the `SigningKey`, `Issuer` and `Audience` from the Tars options

---

## Custom transport (without ASP.NET Core)

For Worker Services, gRPC or other transports, implement `ITokenInputReader` and `ITokenOutputWriter`:

```csharp
// Reader for gRPC metadata
public class GrpcTokenReader : ITokenInputReader
{
    public string? ReadAccessToken(TokenReadContext context)
    {
        if (!context.Headers.TryGetValue("authorization", out var values))
            return null;
        var auth = values.FirstOrDefault();
        return auth?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
            ? auth["Bearer ".Length..]
            : null;
    }

    public string? ReadRefreshToken(TokenReadContext context)
    {
        context.Headers.TryGetValue("x-refresh-token", out var values);
        return values?.FirstOrDefault();
    }
}

// Writer for gRPC — writes to the trailer metadata
public class GrpcTokenWriter : ITokenOutputWriter
{
    public Task WriteAsync(
        TokenWriteContext context,
        TokenResponse tokenResponse,
        TokenDeliveryMode effectiveMode,
        CancellationToken cancellationToken = default)
    {
        context.ResponseHeaders["Authorization"] = $"Bearer {tokenResponse.AccessToken}";
        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            context.ResponseHeaders["X-Refresh-Token"] = tokenResponse.RefreshToken;
        return Task.CompletedTask;
    }
}

// Registration
builder.Services.AddSingleton<ITokenInputReader, GrpcTokenReader>();
builder.Services.AddSingleton<ITokenOutputWriter, GrpcTokenWriter>();
```

---

## TokenReadContext and TokenWriteContext

Both types are framework-agnostic and live in `Abstractions.Transport`:

```csharp
// TokenReadContext — filled in by the adapter from the request
public sealed class TokenReadContext
{
    public IReadOnlyDictionary<string, string[]> Headers { get; init; }
    public IReadOnlyDictionary<string, string> Cookies { get; init; }
    public IReadOnlyDictionary<string, object?> Items { get; init; }
}

// TokenWriteContext — filled in by the ITokenOutputWriter, applied by the adapter on the response
public sealed class TokenWriteContext
{
    public IDictionary<string, string> ResponseHeaders { get; init; }
    public IList<TokenCookieWriteModel> CookiesToAppend { get; init; }
    public IList<string> CookiesToDelete { get; init; }
    public object? Body { get; set; }
}
```

`HttpContextTokenBridge` is responsible for converting `HttpContext` to/from these types:
```csharp
var readContext = HttpContextTokenBridge.CreateReadContext(httpContext);
HttpContextTokenBridge.ApplyWriteContext(httpContext, writeContext);
HttpContextTokenBridge.DeleteAuthCookies(httpContext, aspNetCoreOptions);
```
