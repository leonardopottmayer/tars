# Security / Identity — Endpoints

---

## Built-in endpoints

### Map them all at once

```csharp
app.MapTarsIdentityEndpoints();
```

Automatically includes the OAuth endpoints if `IOAuthUserLinker` is registered in the container.

### Map them individually

```csharp
app.MapTarsIdentitySignInPasswordEndpoint();
app.MapTarsIdentityRequestMagicLinkEndpoint();
app.MapTarsIdentityConsumeMagicLinkEndpoint();
app.MapTarsIdentitySignInApiKeyEndpoint();
app.MapTarsIdentityRefreshEndpoint();
app.MapTarsIdentitySignOutEndpoint();
app.MapTarsIdentityOAuthChallengeEndpoint();
app.MapTarsIdentityOAuthCallbackEndpoint();
```

---

## Endpoint reference

### `POST /identity/sign-in/password`

Authenticates by username/password and issues a JWT + refresh token.

**Request:**
```json
{
  "username": "john@example.com",
  "password": "MyP@ssw0rd123"
}
```

**Dependencies:** `IPasswordAuthenticator`, `ITokenIssuer`, `IRefreshTokenService` (if enabled), `ITokenOutputWriter`, `TokenDeliveryPolicy`.

**Response (BodyOnly):**
```json
{
  "access_token": "eyJhbGc...",
  "refresh_token": "id:token",
  "expires_at": 1748649600,
  "token_type": "Bearer"
}
```

**Response (HeaderOnly):**
```
Authorization: Bearer eyJhbGc...
X-Refresh-Token: id:token
```

---

### `POST /identity/sign-in/magic-link/request`

Generates a magic link token and delegates to `IMagicLinkSender` for delivery.

**Request:**
```json
{
  "target": "john@example.com",
  "returnUrl": "/dashboard"
}
```

**Response:**
```json
{
  "message": "Magic link sent.",
  "expiresAt": "2026-05-28T15:30:00Z"
}
```

**Dependencies:** `IMagicLinkSender`, `IMagicLinkTokenService`, `IMagicLinkTokenStore`.

---

### `POST /identity/sign-in/magic-link/consume`

Consumes the token and issues a JWT. Accepts the token in the body or as a query string.

**Request (body):**
```json
{
  "token": "abc123..."
}
```

**Request (query):**
```
POST /identity/sign-in/magic-link/consume?token=abc123...
```

**Dependencies:** `IMagicLinkIdentityResolver`, `IMagicLinkTokenService`, `ITokenIssuer`.

---

### `POST /identity/sign-in/api-key`

Authenticates an API key and issues a JWT.

**Request:**
```
X-Api-Key: sk_live_abc123...
```

**Dependencies:** `IApiKeyAuthenticator`, `ITokenIssuer`.

---

### `POST /identity/refresh`

Renews the access token using the refresh token.

**Request (header):**
```
X-Refresh-Token: id:token
```

**Request (cookie):**
The `tars.rt` cookie is read automatically if configured.

**Dependencies:** `IRefreshTokenService`, `ITokenIssuer`, `ITokenInputReader`.

**With an authorization handler:**
```csharp
// Register it to re-validate the user on every refresh
builder.Services.AddScoped<IRefreshAuthorizationHandler, MyRefreshAuthorizationHandler>();
```

---

### `POST /identity/sign-out`

Revokes the refresh token and, in stateful mode, the JWT.

**Requires authentication.** Reads the refresh token from the same place as the refresh endpoint.

**Dependencies:** `IRefreshTokenService`, `ITokenInputReader`. Optional: `ITokenRevocationService`, `ISignOutHandler`.

---

### `GET /identity/sign-in/oauth/{provider}`

Starts the OAuth flow for the specified provider.

```
GET /identity/sign-in/oauth/Google
GET /identity/sign-in/oauth/GitHub
```

It can receive `?returnUrl=/dashboard` to redirect after login.

**Dependencies:** The provider must be configured with `SignInScheme = TarsExternalScheme.SchemeName`.

---

### `GET /identity/callback/oauth`

Receives the callback from the OAuth provider, calls `IOAuthUserLinker.LinkAsync()` and issues a JWT.

**Dependencies:** `IOAuthUserLinker`, `ITokenIssuer`, `IRefreshTokenService`.

---

## Customizing the endpoint paths

```json
// appsettings.json
{
  "Tars": {
    "Security": {
      "Identity": {
        "Endpoints": {
          "BasePath": "/api/v1/auth",
          "SignInPasswordPath": "login",
          "RefreshPath": "token/refresh",
          "SignOutPath": "logout",
          "OAuthChallengePath": "social/{provider}",
          "OAuthCallbackPath": "/api/v1/auth/social/callback"
        }
      }
    }
  }
}
```

Result: `POST /api/v1/auth/login`, `POST /api/v1/auth/token/refresh`, etc.

---

## Creating custom endpoints

If you need behavior different from the built-in endpoints, implement your own:

```csharp
// Custom login endpoint with 2FA
app.MapPost("/auth/login", async (
    [FromBody] LoginRequest request,
    [FromServices] IPasswordAuthenticator authenticator,
    [FromServices] ITwoFactorService twoFactor,
    [FromServices] ITokenIssuer tokenIssuer,
    [FromServices] IRefreshTokenService refreshService,
    [FromServices] ITokenOutputWriter outputWriter,
    [FromServices] TokenDeliveryPolicy policy,
    [FromServices] IOptionsMonitor<IdentityOptions> identityOptions,
    [FromServices] IOptionsMonitor<IdentityAspNetCoreOptions> aspNetCoreOptions,
    HttpContext context,
    CancellationToken cancellationToken) =>
{
    var authResult = await authenticator.AuthenticateAsync(
        new PasswordSignInRequest { Username = request.Email, Password = request.Password },
        cancellationToken);

    if (authResult is null)
        return Results.Unauthorized();

    // Check 2FA if enabled
    if (await twoFactor.IsRequiredAsync(authResult.Subject, cancellationToken))
    {
        var challengeToken = await twoFactor.IssueChallengeAsync(authResult.Subject, cancellationToken);
        return Results.Ok(new { requires2fa = true, challengeToken });
    }

    // Issue tokens
    var issued = await tokenIssuer.IssueAsync(authResult, cancellationToken);
    var refreshResult = await refreshService.IssueAsync(
        authResult.Subject, authResult.Claims, null, cancellationToken);

    var response = new TokenResponse
    {
        AccessToken = issued.AccessToken,
        RefreshToken = refreshResult.OpaqueToken,
        ExpiresAt = issued.ExpiresAt,
        TokenType = "Bearer"
    };

    var opts = identityOptions.CurrentValue;
    var aspOpts = aspNetCoreOptions.CurrentValue;
    var effectiveMode = TokenDeliveryAspNetCoreResolver.ResolveEffectiveMode(
        opts, aspOpts, policy,
        hasAuthorizationHeader: false,
        hasAuthCookie: false,
        clientTypeHeaderValue: context.Request.Headers["X-Client-Type"].FirstOrDefault());

    var writeContext = new TokenWriteContext();
    await outputWriter.WriteAsync(writeContext, response, effectiveMode, cancellationToken);
    HttpContextTokenBridge.ApplyWriteContext(context, writeContext);

    return writeContext.Body is not null
        ? Results.Ok(writeContext.Body)
        : Results.Ok(new { message = "Authenticated." });
})
.AllowAnonymous();
```

---

## Testing the endpoints

### cURL — sign-in with header

```bash
curl -X POST https://localhost:5001/identity/sign-in/password \
  -H "Content-Type: application/json" \
  -d '{"username":"admin@example.com","password":"Admin@123"}'
```

### cURL — refresh

```bash
curl -X POST https://localhost:5001/identity/refresh \
  -H "X-Refresh-Token: <refresh-token>"
```

### cURL — sign-out

```bash
curl -X POST https://localhost:5001/identity/sign-out \
  -H "Authorization: Bearer <access-token>" \
  -H "X-Refresh-Token: <refresh-token>"
```

### cURL — magic link request

```bash
curl -X POST https://localhost:5001/identity/sign-in/magic-link/request \
  -H "Content-Type: application/json" \
  -d '{"target":"john@example.com"}'
```

### cURL — magic link consume

```bash
curl -X POST https://localhost:5001/identity/sign-in/magic-link/consume \
  -H "Content-Type: application/json" \
  -d '{"token":"<token-from-link>"}'
```
