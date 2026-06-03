# Security / Identity — Registration Scenarios (DI)

Each scenario is incremental — the more advanced scenarios include everything from the previous ones.

---

## Scenario 1: JWT + password (minimal)

The simplest scenario. The host implements only `IPasswordAuthenticator`.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.AddTarsIdentityOptions(builder.Configuration);
builder.Services.AddTarsIdentityAspNetCoreOptions(builder.Configuration);

// Token
builder.Services.AddTarsIdentityJwtTokenIssuer();    // registers ITokenIssuer
builder.Services.AddTarsIdentityJwtTokenValidator(); // registers ITokenValidator
builder.Services.AddTarsIdentityTokenDeliveryPolicy();

// Authenticator (implemented by the host)
builder.Services.AddScoped<IPasswordAuthenticator, MyPasswordAuthenticator>();

// ASP.NET Core
builder.Services.AddAuthentication()
    .AddTarsIdentityJwtBearer();

builder.Services.AddTarsIdentityAspNetCoreTokenTransport();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapTarsIdentitySignInPasswordEndpoint();
app.Run();
```

```json
// appsettings.json (minimal)
{
  "Tars": {
    "Security": {
      "Identity": {
        "Jwt": {
          "SigningKey": "secret-key-of-at-least-32-chars!!",
          "Issuer": "my-api",
          "Audience": "my-api"
        },
        "TokenDelivery": {
          "Mode": "HeaderOnly"
        }
      }
    }
  }
}
```

```csharp
// MyPasswordAuthenticator.cs
public class MyPasswordAuthenticator : IPasswordAuthenticator
{
    private readonly IUserRepository _repo;
    private readonly IPasswordHasher _hasher;

    public MyPasswordAuthenticator(IUserRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async ValueTask<AuthenticationResult?> AuthenticateAsync(
        PasswordSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _repo.FindByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            return null;

        return new AuthenticationResult
        {
            Subject = user.Id.ToString(),
            Claims =
            [
                new ClaimData("email", user.Email),
                new ClaimData("role", user.Role)
            ]
        };
    }
}
```

---

## Scenario 2: JWT + password + refresh token

```csharp
// Program.cs
builder.Services.AddTarsIdentityOptions(builder.Configuration);
builder.Services.AddTarsIdentityAspNetCoreOptions(builder.Configuration);

builder.Services.AddTarsIdentityJwtTokenIssuer();
builder.Services.AddTarsIdentityJwtTokenValidator();
builder.Services.AddTarsIdentityTokenDeliveryPolicy();
builder.Services.AddTarsIdentityRefreshTokenService();       // IRefreshTokenService
builder.Services.AddTarsIdentityInMemoryRefreshTokenStore(); // or a custom store

builder.Services.AddScoped<IPasswordAuthenticator, MyPasswordAuthenticator>();

builder.Services.AddAuthentication()
    .AddTarsIdentityJwtBearer();

builder.Services.AddTarsIdentityAspNetCoreTokenTransport();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapTarsIdentitySignInPasswordEndpoint();
app.MapTarsIdentityRefreshEndpoint();
app.MapTarsIdentitySignOutEndpoint();
app.Run();
```

```json
// appsettings.json
{
  "Tars": {
    "Security": {
      "Identity": {
        "Jwt": {
          "SigningKey": "secret-key-of-at-least-32-chars!!",
          "Issuer": "my-api",
          "Audience": "my-api",
          "AccessTokenLifetime": "00:15:00"
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
        }
      }
    }
  }
}
```

### With a custom store (Redis or database)

For multi-instance production, replace the in-memory store:

```csharp
// Custom store
public class RedisRefreshTokenStore : IRefreshTokenStore
{
    private readonly IDatabase _redis;

    public RedisRefreshTokenStore(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async ValueTask StoreAsync(
        string tokenId,
        string subject,
        IReadOnlyList<ClaimData> claims,
        DateTimeOffset expiresAt,
        IReadOnlyDictionary<string, object?>? metadata,
        CancellationToken cancellationToken = default)
    {
        var payload = new RefreshTokenPayloadDto
        {
            Subject = subject,
            Claims = claims.Select(c => new[] { c.Type, c.Value }).ToList(),
            Metadata = metadata
        };
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        await _redis.StringSetAsync($"rt:{tokenId}", JsonSerializer.Serialize(payload), ttl);
    }

    // ... implement the remaining methods
}

// Registration
builder.Services.AddSingleton<IRefreshTokenStore, RedisRefreshTokenStore>();
```

---

## Scenario 3: JWT + password + refresh + magic link + API key

```csharp
// Program.cs
builder.Services.AddTarsIdentityOptions(builder.Configuration);
builder.Services.AddTarsIdentityAspNetCoreOptions(builder.Configuration);

builder.Services.AddTarsIdentityJwtTokenIssuer();
builder.Services.AddTarsIdentityJwtTokenValidator();
builder.Services.AddTarsIdentityTokenDeliveryPolicy();
builder.Services.AddTarsIdentityRefreshTokenService();
builder.Services.AddTarsIdentityInMemoryRefreshTokenStore();
builder.Services.AddTarsIdentityMagicLinkTokenService();
builder.Services.AddTarsIdentityInMemoryMagicLinkTokenStore();

// Host contracts
builder.Services.AddScoped<IPasswordAuthenticator, MyPasswordAuthenticator>();
builder.Services.AddScoped<IMagicLinkIdentityResolver, MyMagicLinkIdentityResolver>();
builder.Services.AddScoped<IMagicLinkSender, EmailMagicLinkSender>();
builder.Services.AddScoped<IApiKeyAuthenticator, MyApiKeyAuthenticator>();

builder.Services.AddAuthentication()
    .AddTarsIdentityJwtBearer();

builder.Services.AddTarsIdentityAspNetCoreTokenTransport();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapTarsIdentityEndpoints(); // all 6 endpoints
app.Run();
```

```csharp
// EmailMagicLinkSender.cs
public class EmailMagicLinkSender : IMagicLinkSender
{
    private readonly IEmailService _email;

    public EmailMagicLinkSender(IEmailService email) => _email = email;

    public async Task SendAsync(
        string target,
        string linkUrl,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        await _email.SendAsync(
            to: target,
            subject: "Your access link",
            body: $"<a href=\"{linkUrl}\">Click to sign in</a> (valid until {expiresAt:g})",
            cancellationToken: cancellationToken);
    }
}

// MyMagicLinkIdentityResolver.cs
public class MyMagicLinkIdentityResolver : IMagicLinkIdentityResolver
{
    private readonly IUserRepository _repo;

    public MyMagicLinkIdentityResolver(IUserRepository repo) => _repo = repo;

    public async ValueTask<AuthenticationResult?> ResolveAsync(
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken = default)
    {
        var target = payload.TryGetValue("target", out var t) ? t?.ToString() : null;
        if (string.IsNullOrEmpty(target))
            return null;

        var user = await _repo.FindByEmailAsync(target, cancellationToken);
        if (user is null)
            return null;

        return new AuthenticationResult
        {
            Subject = user.Id.ToString(),
            Claims = [new ClaimData("email", user.Email)]
        };
    }
}
```

---

## Scenario 4: JWT + password + OAuth (Google / GitHub)

```csharp
// Program.cs
builder.Services.AddTarsIdentityOptions(builder.Configuration);
builder.Services.AddTarsIdentityAspNetCoreOptions(builder.Configuration);

builder.Services.AddTarsIdentityJwtTokenIssuer();
builder.Services.AddTarsIdentityJwtTokenValidator();
builder.Services.AddTarsIdentityTokenDeliveryPolicy();
builder.Services.AddTarsIdentityRefreshTokenService();
builder.Services.AddTarsIdentityInMemoryRefreshTokenStore();

builder.Services.AddScoped<IPasswordAuthenticator, MyPasswordAuthenticator>();
builder.Services.AddScoped<IOAuthUserLinker, MyOAuthUserLinker>();

builder.Services.AddAuthentication()
    .AddTarsIdentityJwtBearer()
    .AddTarsIdentityExternalScheme()         // temporary cookie scheme
    .AddGoogle(options =>
    {
        options.SignInScheme = TarsExternalScheme.SchemeName;
        options.ClientId = builder.Configuration["Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
    });

builder.Services.AddTarsIdentityAspNetCoreTokenTransport();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapTarsIdentityEndpoints(); // includes OAuth automatically (IOAuthUserLinker registered)
app.Run();
```

```csharp
// MyOAuthUserLinker.cs
public class MyOAuthUserLinker : IOAuthUserLinker
{
    private readonly IUserRepository _repo;

    public MyOAuthUserLinker(IUserRepository repo) => _repo = repo;

    public async ValueTask<AuthenticationResult?> LinkAsync(
        string provider,
        IReadOnlyDictionary<string, string?> externalClaims,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        var email = externalClaims.TryGetValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", out var e)
            ? e
            : externalClaims.TryGetValue("email", out var e2) ? e2 : null;

        if (string.IsNullOrEmpty(email))
            return null;

        // Find or create the local user
        var user = await _repo.FindByEmailAsync(email, cancellationToken)
            ?? await _repo.CreateFromOAuthAsync(provider, externalId, email, cancellationToken);

        if (user is null)
            return null;

        return new AuthenticationResult
        {
            Subject = user.Id.ToString(),
            Claims =
            [
                new ClaimData("email", user.Email),
                new ClaimData("provider", provider)
            ]
        };
    }
}
```

---

## Scenario 5: Stateful revocation

Add to any scenario above:

```csharp
// JTI blacklist
builder.Services.AddTarsIdentityTokenRevocationService();
builder.Services.AddTarsIdentityInMemoryTokenRevocationStore();
// or a custom store:
// builder.Services.AddScoped<ITokenRevocationStore, RedisTokenRevocationStore>();
```

```json
// appsettings.json
"Revocation": {
  "SignOutMode": "Stateful",
  "Strategy": "JtiBlacklist",
  "StatefulRevocationEnabled": true
}
```

For Session Version (no need to keep a blacklist):

```json
"Revocation": {
  "SignOutMode": "Stateful",
  "Strategy": "SessionVersion",
  "StatefulRevocationEnabled": true
}
```

---

## Scenario 6: Worker service / console (without ASP.NET Core)

Use only the Runtime with no dependency on AspNetCore:

```csharp
// Program.cs (Worker Service)
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddTarsIdentityOptions(ctx.Configuration);
        services.AddTarsIdentityJwtTokenIssuer();    // ITokenIssuer
        services.AddTarsIdentityJwtTokenValidator(); // ITokenValidator

        // Implement ITokenInputReader for the service's transport (e.g. gRPC, messaging)
        services.AddSingleton<ITokenInputReader, GrpcMetadataTokenReader>();
    })
    .Build();

await host.RunAsync();
```

```csharp
// GrpcMetadataTokenReader.cs — example of a custom reader for gRPC
public class GrpcMetadataTokenReader : ITokenInputReader
{
    public string? ReadAccessToken(TokenReadContext context)
    {
        if (!context.Headers.TryGetValue("authorization", out var values))
            return null;
        var auth = values.FirstOrDefault();
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        return auth["Bearer ".Length..];
    }

    public string? ReadRefreshToken(TokenReadContext context)
    {
        context.Headers.TryGetValue("x-refresh-token", out var values);
        return values?.FirstOrDefault();
    }
}
```
