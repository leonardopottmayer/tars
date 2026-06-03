# Security / Identity — Overview

The Tars Identity module covers JWT issuance and validation, refresh tokens with rotation and reuse detection, stateful revocation, magic link, API key authentication, the OAuth flow and token delivery via header, cookie or hybrid mode.

---

## Packages

| Package | Role |
|---|---|
| `Pottmayer.Tars.Security.Identity.Abstractions` | Contracts, DTOs, enums, stores, transport types |
| `Pottmayer.Tars.Security.Identity` | JWT, refresh, magic link and revocation implementations, optional base classes |
| `Pottmayer.Tars.Security.Identity.AspNetCore` | Middleware, Minimal API endpoints, token read/write via HttpContext |

---

## Separation of responsibilities

| Responsibility | Package |
|---|---|
| Token contracts (`ITokenIssuer`, `ITokenValidator`) | Abstractions |
| Service contracts (`IRefreshTokenService`, `IMagicLinkTokenService`) | Abstractions |
| Flow contracts (`IPasswordAuthenticator`, `IOAuthUserLinker`, etc.) | Abstractions |
| Transport contracts (`ITokenInputReader`, `ITokenOutputWriter`) | Abstractions |
| Neutral transport types (`TokenReadContext`, `TokenWriteContext`) | Abstractions |
| JWT implementations | `Security.Identity` |
| Refresh, magic link and revocation implementations | `Security.Identity` |
| Optional base class `PasswordAuthenticatorBase<TUser>` | `Security.Identity` |
| In-memory stores (dev/single-instance) | `Security.Identity` |
| Token read via HTTP (header, cookie) | `Security.Identity.AspNetCore` |
| Token write via HTTP (cookie, header, body) | `Security.Identity.AspNetCore` |
| Minimal API endpoints | `Security.Identity.AspNetCore` |

---

## Supported flows

### Password (username + password)

The host implements `IPasswordAuthenticator`. The framework issues a JWT + refresh token and writes them to the configured transport.

```
POST /identity/sign-in/password
Body: { "username": "...", "password": "..." }
```

**Alternative with a base class:** inherit from `PasswordAuthenticatorBase<TUser>` and implement only the password check — lookup and claims are delegated to `IUserStore<TUser>` and `IClaimsProvider<TUser>`.

---

### Magic Link

Two steps: request the link and consume the token.

```
POST /identity/sign-in/magic-link/request
Body: { "target": "user@example.com" }

POST /identity/sign-in/magic-link/consume
Body: { "token": "<token>" }
```

The host implements `IMagicLinkSender` (to send the email/SMS) and `IMagicLinkIdentityResolver` (to map the token payload to an `AuthenticationResult`).

---

### API Key

```
POST /identity/sign-in/api-key
Header: X-Api-Key: <key>
```

The host implements `IApiKeyAuthenticator`. Additionally, the `ApiKeyAuthenticationHandler` can be used as a direct scheme without JWT issuance.

---

### OAuth / External providers

Two endpoints:

```
GET /identity/sign-in/oauth/{provider}    ← challenge (redirects to Google/GitHub/etc.)
GET /identity/callback/oauth              ← callback (receives identity, issues JWT)
```

The host implements `IOAuthUserLinker` to map the external identity to the local user.

---

### Refresh Token

```
POST /identity/refresh
Header: X-Refresh-Token: <token>   (or cookie, depending on the mode)
```

Supports automatic rotation (the old token is invalidated, a new one is issued) and reuse detection (if an already-consumed token is reused, all of the user's tokens are revoked).

---

### Sign Out

```
POST /identity/sign-out
Authorization: Bearer <token>
```

Revokes the refresh token. In stateful mode it also revokes the JTI in the blacklist or increments the session version.

---

## Contracts the host implements

| Contract | When to use |
|---|---|
| `IPasswordAuthenticator` | Password flow (direct implementation) |
| `PasswordAuthenticatorBase<TUser>` | Password flow (optional base class) |
| `IApiKeyAuthenticator` | sign-in/api-key endpoint |
| `IApiKeyValidator` | Direct API key handler |
| `IMagicLinkSender` | Sending the link by email/SMS |
| `IMagicLinkIdentityResolver` | Mapping payload → user |
| `IOAuthUserLinker` | Mapping the OAuth identity → local user |
| `IRefreshAuthorizationHandler` | Claim enrichment on refresh |
| `ISignOutHandler` | Side-effects on sign-out (e.g. auditing) |
| `IRefreshTokenStore` | Refresh token store (required if using refresh) |
| `ITokenRevocationStore` | Stateful revocation store |
| `IMagicLinkTokenStore` | Magic link token store |
| `IUserStore<TUser>` | Used by `PasswordAuthenticatorBase<TUser>` |
| `IClaimsProvider<TUser>` | Used by `PasswordAuthenticatorBase<TUser>` |

---

## In-memory stores (dev/single-instance)

The `Security.Identity` package includes in-memory stores based on `ConcurrentDictionary` for development use or single-instance applications:

```csharp
services.AddTarsIdentityInMemoryRefreshTokenStore();
services.AddTarsIdentityInMemoryMagicLinkTokenStore();
services.AddTarsIdentityInMemoryTokenRevocationStore();
```

For multi-instance production, implement the store contracts (e.g. Redis, relational database).

---

## Links

- [Complete configuration](./configuration.md)
- [Registration scenarios (DI)](./scenarios.md)
- [Implementing the authentication flows](./authentication-flows.md)
- [Endpoints — built-in and custom](./endpoints.md)
- [Token delivery (header, cookie, hybrid)](./token-delivery.md)
- [OAuth and external providers](./oauth.md)
- [Standardizing 401/403 auth responses (`ConfigureTarsIdentityProblemResponses`)](../../web/error-mapping.md#auth-challengeforbidden-responses-401403)
