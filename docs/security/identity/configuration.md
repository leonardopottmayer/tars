# Security / Identity — Configuration

## Configuration section

All options live under:

```json
"Tars": {
  "Security": {
    "Identity": { ... }
  }
}
```

Registering the options in DI:

```csharp
// Core options (JWT, refresh, revocation, magic link, limits)
builder.Services.AddTarsIdentityOptions(builder.Configuration);

// ASP.NET Core options (cookies, endpoints, API key, delivery)
builder.Services.AddTarsIdentityAspNetCoreOptions(builder.Configuration);
```

---

## Core options

### `Jwt`

```json
"Jwt": {
  "SigningKey": "secret-key-of-at-least-32-characters!!",
  "Issuer": "my-api",
  "Audience": "my-api",
  "AccessTokenLifetime": "00:15:00",
  "ClockSkew": "00:01:00"
}
```

| Field | Type | Default | Description |
|---|---|---|---|
| `SigningKey` | string | — | **Required.** HMAC-SHA256 key (min. 32 characters). |
| `Issuer` | string | — | The JWT `iss` claim. |
| `Audience` | string | — | The JWT `aud` claim. |
| `AccessTokenLifetime` | TimeSpan | `00:15:00` | Access token lifetime. |
| `ClockSkew` | TimeSpan | `00:05:00` | Expiration tolerance margin. |

---

### `RefreshToken`

```json
"RefreshToken": {
  "Enabled": true,
  "Lifetime": "7.00:00:00",
  "RotationEnabled": true,
  "ReuseDetectionEnabled": true
}
```

| Field | Type | Default | Description |
|---|---|---|---|
| `Enabled` | bool | `false` | Enables refresh token issuance. |
| `Lifetime` | TimeSpan | `7.00:00:00` | Refresh token lifetime. |
| `RotationEnabled` | bool | `true` | Invalidates the consumed token and issues a new one on each use. |
| `ReuseDetectionEnabled` | bool | `true` | If an already-consumed token is reused, revokes all of the user's tokens. |

---

### `TokenDelivery`

```json
"TokenDelivery": {
  "Mode": "Hybrid"
}
```

Available modes:

| Mode | Behavior |
|---|---|
| `HeaderOnly` | Tokens delivered in the `Authorization` and `X-Refresh-Token` headers. |
| `CookieOnly` | Tokens delivered in HttpOnly cookies. |
| `BodyOnly` | Tokens delivered in the JSON body. |
| `Hybrid` | Decides automatically based on the `X-Client-Type` header or the presence of Authorization/cookie. |

---

### `Revocation`

```json
"Revocation": {
  "SignOutMode": "Stateful",
  "Strategy": "JtiBlacklist",
  "StatefulRevocationEnabled": true
}
```

| Field | Type | Description |
|---|---|---|
| `SignOutMode` | `Stateless` \| `Stateful` | `Stateless` revokes only the refresh token. `Stateful` also revokes the JWT. |
| `Strategy` | `JtiBlacklist` \| `SessionVersion` | `JtiBlacklist` blacklists the JTI; `SessionVersion` increments `sv` in the store. |
| `StatefulRevocationEnabled` | bool | Enables stateful revocation. |

---

### `MagicLink`

```json
"MagicLink": {
  "TokenLifetime": "00:15:00"
}
```

---

### `InputLimits`

Protection against DoS on the authentication inputs:

```json
"InputLimits": {
  "MaxUsernameLength": 256,
  "MaxPasswordLength": 1024,
  "MaxMagicLinkTargetLength": 512,
  "MaxReturnUrlLength": 2048,
  "MaxApiKeyLength": 512
}
```

---

## ASP.NET Core options

### `Jwt`

```json
"Jwt": {
  "RequireHttpsMetadata": true
}
```

Disable it in development: `"RequireHttpsMetadata": false`.

---

### `Cookie`

```json
"Cookie": {
  "AccessTokenCookieName": "tars.at",
  "RefreshTokenCookieName": "tars.rt",
  "Path": "/",
  "SameSite": 1,
  "HttpOnly": true,
  "SecurePolicy": true
}
```

`SameSite`: `0` = None, `1` = Lax, `2` = Strict, `-1` = Unspecified.

---

### `RefreshToken`

```json
"RefreshToken": {
  "RefreshTokenHeaderName": "X-Refresh-Token"
}
```

---

### `TokenDelivery`

```json
"TokenDelivery": {
  "HybridClientTypeHeader": "X-Client-Type",
  "HybridCookieClientTypeValue": "web",
  "HybridHeaderClientTypeValue": "api"
}
```

When `Mode = Hybrid`, the server inspects the `X-Client-Type` header:
- `web` → deliver via cookie
- `api` → deliver via header

---

### `ApiKey`

```json
"ApiKey": {
  "HeaderName": "X-Api-Key",
  "QueryParameterName": null
}
```

---

### `Endpoints`

```json
"Endpoints": {
  "BasePath": "/identity",
  "SignInPasswordPath": "sign-in/password",
  "RequestMagicLinkPath": "sign-in/magic-link/request",
  "ConsumeMagicLinkPath": "sign-in/magic-link/consume",
  "SignInApiKeyPath": "sign-in/api-key",
  "RefreshPath": "refresh",
  "SignOutPath": "sign-out",
  "OAuthChallengePath": "sign-in/oauth/{provider}",
  "OAuthCallbackPath": "/identity/callback/oauth"
}
```

---

### `MagicLink`

```json
"MagicLink": {
  "BaseUrl": "https://my-app.example/sign-in/magic-link",
  "TokenQueryParameter": "token"
}
```

The link sent to the user will be: `https://my-app.example/sign-in/magic-link?token=<token>`.

---

## Complete `appsettings` examples

### Scenario 1 — JWT + password + refresh (header)

```json
{
  "Tars": {
    "Security": {
      "Identity": {
        "Jwt": {
          "SigningKey": "secret-key-of-at-least-32-chars!!",
          "Issuer": "my-api",
          "Audience": "my-api",
          "AccessTokenLifetime": "00:15:00",
          "ClockSkew": "00:01:00"
        },
        "RefreshToken": {
          "Enabled": true,
          "Lifetime": "7.00:00:00",
          "RotationEnabled": true,
          "ReuseDetectionEnabled": true,
          "RefreshTokenHeaderName": "X-Refresh-Token"
        },
        "TokenDelivery": {
          "Mode": "HeaderOnly"
        },
        "Jwt": {
          "RequireHttpsMetadata": false
        }
      }
    }
  }
}
```

---

### Scenario 2 — JWT + cookie (browser-only SPA)

```json
{
  "Tars": {
    "Security": {
      "Identity": {
        "Jwt": {
          "SigningKey": "secret-key-of-at-least-32-chars!!",
          "Issuer": "my-app",
          "Audience": "my-app",
          "AccessTokenLifetime": "00:15:00"
        },
        "RefreshToken": {
          "Enabled": true,
          "Lifetime": "7.00:00:00",
          "RotationEnabled": true,
          "ReuseDetectionEnabled": true
        },
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
        },
        "Revocation": {
          "SignOutMode": "Stateful",
          "Strategy": "JtiBlacklist",
          "StatefulRevocationEnabled": true
        }
      }
    }
  }
}
```

---

### Scenario 3 — Hybrid (SPA + mobile API)

```json
{
  "Tars": {
    "Security": {
      "Identity": {
        "Jwt": {
          "SigningKey": "secret-key-of-at-least-32-chars!!",
          "Issuer": "my-app",
          "Audience": "my-app",
          "AccessTokenLifetime": "00:15:00"
        },
        "RefreshToken": {
          "Enabled": true,
          "Lifetime": "30.00:00:00",
          "RotationEnabled": true,
          "ReuseDetectionEnabled": true
        },
        "TokenDelivery": {
          "Mode": "Hybrid",
          "HybridClientTypeHeader": "X-Client-Type",
          "HybridCookieClientTypeValue": "web",
          "HybridHeaderClientTypeValue": "api"
        },
        "Cookie": {
          "AccessTokenCookieName": "app.at",
          "RefreshTokenCookieName": "app.rt",
          "SameSite": 1,
          "HttpOnly": true,
          "SecurePolicy": true
        },
        "RefreshToken": {
          "RefreshTokenHeaderName": "X-Refresh-Token"
        }
      }
    }
  }
}
```

---

### Scenario 4 — Magic link + OAuth

```json
{
  "Tars": {
    "Security": {
      "Identity": {
        "Jwt": {
          "SigningKey": "secret-key-of-at-least-32-chars!!",
          "Issuer": "my-app",
          "Audience": "my-app",
          "AccessTokenLifetime": "00:30:00"
        },
        "RefreshToken": {
          "Enabled": true,
          "Lifetime": "7.00:00:00",
          "RotationEnabled": true,
          "ReuseDetectionEnabled": true
        },
        "MagicLink": {
          "TokenLifetime": "00:15:00",
          "BaseUrl": "https://my-app.com/auth/magic",
          "TokenQueryParameter": "token"
        },
        "TokenDelivery": {
          "Mode": "CookieOnly"
        }
      }
    }
  }
}
```

---

### Scenario 5 — Revocation by Session Version

Useful when you want to invalidate all of a user's tokens without keeping a growing blacklist of JTIs:

```json
{
  "Tars": {
    "Security": {
      "Identity": {
        "Jwt": {
          "SigningKey": "secret-key-of-at-least-32-chars!!",
          "Issuer": "my-app",
          "Audience": "my-app",
          "AccessTokenLifetime": "00:05:00"
        },
        "Revocation": {
          "SignOutMode": "Stateful",
          "Strategy": "SessionVersion",
          "StatefulRevocationEnabled": true
        }
      }
    }
  }
}
```

With `Strategy = SessionVersion`, the JWT includes an `sv` (session version) claim. On sign-out, the store increments the version. Tokens issued before the sign-out have a lower `sv` and are rejected during validation.
