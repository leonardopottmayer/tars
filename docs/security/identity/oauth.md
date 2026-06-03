# Security / Identity — OAuth and External Providers

The module supports any OAuth 2.0 / OIDC provider (Google, GitHub, Microsoft, Facebook, etc.) via the ASP.NET Core external scheme.

---

## How it works

```
[User] → GET /identity/sign-in/oauth/Google
       → [Redirect to Google]
       → [Google redirects to /identity/callback/oauth]
       → [Framework authenticates in the TarsExternalCookie scheme]
       → [IOAuthUserLinker.LinkAsync()]
       → [JWT + refresh token issued]
```

1. The challenge endpoint (`/identity/sign-in/oauth/{provider}`) issues a `ChallengeResult` for the provider.
2. The provider redirects to `/identity/callback/oauth` with the authorization code.
3. ASP.NET Core authenticates the code and deposits the identity in the `TarsExternalCookie` scheme.
4. The callback endpoint reads the identity from `TarsExternalCookie`, deletes the temporary cookie and calls `IOAuthUserLinker.LinkAsync()`.
5. The linker decides whether to create, link or reject the user and returns an `AuthenticationResult`.
6. The framework issues a JWT + refresh token with the configured options.

---

## Complete setup

### 1. Install NuGet packages

```xml
<!-- Google -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="10.0.*" />

<!-- GitHub (via the generic ASP.NET Core OAuth) -->
<PackageReference Include="AspNet.Security.OAuth.GitHub" Version="9.*" />

<!-- Microsoft -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.MicrosoftAccount" Version="10.0.*" />
```

### 2. Configure appsettings

```json
{
  "Tars": {
    "Security": {
      "Identity": {
        "Jwt": {
          "SigningKey": "secret-key-of-at-least-32-chars!!",
          "Issuer": "my-app",
          "Audience": "my-app"
        },
        "RefreshToken": {
          "Enabled": true,
          "Lifetime": "7.00:00:00",
          "RotationEnabled": true
        },
        "TokenDelivery": {
          "Mode": "CookieOnly"
        }
      }
    }
  },
  "Google": {
    "ClientId": "1234567890-abc.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-..."
  },
  "GitHub": {
    "ClientId": "Iv1.abc123",
    "ClientSecret": "abc123..."
  }
}
```

### 3. Register in DI

```csharp
builder.Services.AddTarsIdentityOptions(builder.Configuration);
builder.Services.AddTarsIdentityAspNetCoreOptions(builder.Configuration);

builder.Services.AddTarsIdentityJwtTokenIssuer();
builder.Services.AddTarsIdentityJwtTokenValidator();
builder.Services.AddTarsIdentityTokenDeliveryPolicy();
builder.Services.AddTarsIdentityRefreshTokenService();
builder.Services.AddTarsIdentityInMemoryRefreshTokenStore();

// IOAuthUserLinker — implemented by the host
builder.Services.AddScoped<IOAuthUserLinker, MyOAuthUserLinker>();

builder.Services.AddAuthentication()
    .AddTarsIdentityJwtBearer()
    .AddTarsIdentityExternalScheme()   // TarsExternalCookie scheme (required)
    .AddGoogle(options =>
    {
        options.SignInScheme = TarsExternalScheme.SchemeName;
        options.ClientId = builder.Configuration["Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
        // Extra scopes
        options.Scope.Add("profile");
    })
    .AddGitHub(options =>              // requires AspNet.Security.OAuth.GitHub
    {
        options.SignInScheme = TarsExternalScheme.SchemeName;
        options.ClientId = builder.Configuration["GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["GitHub:ClientSecret"]!;
        options.Scope.Add("user:email");
    });

builder.Services.AddTarsIdentityAspNetCoreTokenTransport();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapTarsIdentityEndpoints(); // includes /sign-in/oauth/{provider} and /callback/oauth
app.Run();
```

### 4. Implement `IOAuthUserLinker`

```csharp
public class MyOAuthUserLinker : IOAuthUserLinker
{
    private readonly IUserRepository _repo;
    private readonly ILogger<MyOAuthUserLinker> _logger;

    public MyOAuthUserLinker(IUserRepository repo, ILogger<MyOAuthUserLinker> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async ValueTask<AuthenticationResult?> LinkAsync(
        string provider,
        IReadOnlyDictionary<string, string?> externalClaims,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        // Extract the email from the provider
        var email = GetEmail(externalClaims);
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("OAuth provider {Provider} did not return an email", provider);
            return null;
        }

        // Look up the user by email or create one
        var user = await _repo.FindByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            // Create the user automatically or block
            user = await _repo.CreateFromOAuthAsync(new CreateOAuthUserCommand
            {
                Email = email,
                Provider = provider,
                ExternalId = externalId,
                DisplayName = GetDisplayName(externalClaims)
            }, cancellationToken);
        }
        else
        {
            // Link the external account to the existing user (if not already linked)
            await _repo.EnsureExternalLoginAsync(user.Id, provider, externalId, cancellationToken);
        }

        if (!user.IsActive)
            return null; // disabled account

        return new AuthenticationResult
        {
            Subject = user.Id.ToString(),
            Claims =
            [
                new ClaimData("email", user.Email),
                new ClaimData("name", user.DisplayName ?? ""),
                new ClaimData("provider", provider),
                new ClaimData("role", user.Role)
            ]
        };
    }

    private static string? GetEmail(IReadOnlyDictionary<string, string?> claims)
    {
        // Google / OIDC standard
        if (claims.TryGetValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", out var e1) && !string.IsNullOrEmpty(e1))
            return e1;
        // GitHub / others
        if (claims.TryGetValue("urn:github:email", out var e2) && !string.IsNullOrEmpty(e2))
            return e2;
        if (claims.TryGetValue("email", out var e3) && !string.IsNullOrEmpty(e3))
            return e3;
        return null;
    }

    private static string? GetDisplayName(IReadOnlyDictionary<string, string?> claims)
    {
        claims.TryGetValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", out var name);
        return name;
    }
}
```

---

## Using only some providers

The OAuth endpoints are mapped conditionally, only if `IOAuthUserLinker` is registered:

```csharp
// With MapTarsIdentityEndpoints() — includes OAuth automatically
app.MapTarsIdentityEndpoints();

// Or map explicitly only what you need
app.MapTarsIdentitySignInPasswordEndpoint();
app.MapTarsIdentityRefreshEndpoint();
app.MapTarsIdentitySignOutEndpoint();
app.MapTarsIdentityOAuthChallengeEndpoint();  // GET /identity/sign-in/oauth/{provider}
app.MapTarsIdentityOAuthCallbackEndpoint();   // GET /identity/callback/oauth
```

---

## Configuring the callback URL in the provider

The callback URL you register in the provider's console must point to `OAuthCallbackPath`:

```
https://my-api.com/identity/callback/oauth
```

If you customized the path:
```json
"Endpoints": {
  "OAuthCallbackPath": "/api/v1/auth/social/callback"
}
```

Register it in the provider:
```
https://my-api.com/api/v1/auth/social/callback
```

---

## ReturnUrl after OAuth login

The challenge endpoint accepts `?returnUrl=/dashboard`:

```
GET /identity/sign-in/oauth/Google?returnUrl=/dashboard
```

The value is preserved in `AuthenticationProperties` and can be read by the `IOAuthUserLinker`:

```csharp
// In the callback, the returnUrl lives in externalClaims via Properties.Items
// If you need to redirect, implement your own callback endpoint
```

---

## OAuth with cookies (SPA)

For SPAs that use `CookieOnly` mode, the OAuth flow ends with the cookies being set, and you can redirect to the SPA:

```csharp
// Custom callback endpoint to redirect after OAuth
app.MapGet("/auth/oauth-callback", async (
    HttpContext context,
    [FromServices] IOAuthUserLinker linker,
    [FromServices] ITokenIssuer tokenIssuer,
    [FromServices] IRefreshTokenService refreshService,
    [FromQuery] string? returnUrl) =>
{
    var result = await context.AuthenticateAsync(TarsExternalScheme.SchemeName);
    if (!result.Succeeded)
        return Results.Redirect("/login?error=oauth_failed");

    await context.SignOutAsync(TarsExternalScheme.SchemeName);

    var provider = result.Properties?.Items.TryGetValue("provider", out var p) == true ? p ?? "unknown" : "unknown";
    var externalId = result.Principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    var externalClaims = result.Principal.Claims
        .GroupBy(c => c.Type)
        .ToDictionary(g => g.Key, g => (string?)g.First().Value);

    var authResult = await linker.LinkAsync(provider, externalClaims, externalId);
    if (authResult is null)
        return Results.Redirect("/login?error=account_not_allowed");

    // Issue tokens as cookies
    var issued = await tokenIssuer.IssueAsync(authResult);
    var refresh = await refreshService.IssueAsync(authResult.Subject, authResult.Claims, null);

    context.Response.Cookies.Append("app.at", issued.AccessToken, new CookieOptions
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.FromUnixTimeSeconds(issued.ExpiresAt)
    });
    context.Response.Cookies.Append("app.rt", refresh.OpaqueToken, new CookieOptions
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddDays(7)
    });

    var destination = !string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
        ? returnUrl
        : "/";

    return Results.Redirect(destination);
})
.AllowAnonymous();
```

---

## Microsoft Account

```csharp
.AddMicrosoftAccount(options =>
{
    options.SignInScheme = TarsExternalScheme.SchemeName;
    options.ClientId = builder.Configuration["Microsoft:ClientId"]!;
    options.ClientSecret = builder.Configuration["Microsoft:ClientSecret"]!;
})
```

Provider name in the challenge: `GET /identity/sign-in/oauth/Microsoft`

---

## Adding `AddTarsIdentityExternalScheme` in a production environment

The external scheme uses a short-lived cookie (15 minutes by default) to capture the provider's identity. This is safe because:

1. The cookie is HttpOnly and Secure
2. SameSite is None (required for the cross-origin OAuth redirect)
3. The cookie is deleted immediately after the callback

If you need to customize it:

```csharp
builder.Services.AddAuthentication()
    .AddTarsIdentityExternalScheme(callbackPath: "/identity/callback/oauth")
    // or configure manually:
    .AddCookie(TarsExternalScheme.SchemeName, options =>
    {
        options.Cookie.Name = TarsExternalScheme.SchemeName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });
```
