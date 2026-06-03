# Security / Identity — Implementing the Authentication Flows

---

## Password flow — `IPasswordAuthenticator`

### Direct implementation

```csharp
public class MyPasswordAuthenticator : IPasswordAuthenticator
{
    private readonly IUserRepository _repo;
    private readonly IPasswordHasher<ApplicationUser> _hasher;

    public MyPasswordAuthenticator(IUserRepository repo, IPasswordHasher<ApplicationUser> hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async ValueTask<AuthenticationResult?> AuthenticateAsync(
        PasswordSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _repo.FindByUsernameOrEmailAsync(request.Username, cancellationToken);
        if (user is null)
            return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        return new AuthenticationResult
        {
            Subject = user.Id.ToString(),
            Claims =
            [
                new ClaimData(ClaimTypes.Email, user.Email),
                new ClaimData(ClaimTypes.Role, user.Role),
                new ClaimData("tenant", user.TenantId)
            ],
            SessionVersion = user.SessionVersion // includes the sv claim for revocation
        };
    }
}
```

### Via the base class `PasswordAuthenticatorBase<TUser>`

The base class handles the lookup and claims. You implement only the password verification:

```csharp
// 1. UserStore
public class UserStore : IUserStore<ApplicationUser>
{
    private readonly AppDbContext _db;

    public UserStore(AppDbContext db) => _db = db;

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct = default)
        => _db.Users.FindAsync([Guid.Parse(userId)], ct).AsTask();

    public Task<ApplicationUser?> FindByUsernameAsync(string username, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
}

// 2. ClaimsProvider
public class UserClaimsProvider : IClaimsProvider<ApplicationUser>
{
    public Task<IReadOnlyList<ClaimData>> GetClaimsAsync(ApplicationUser user, CancellationToken ct = default)
    {
        IReadOnlyList<ClaimData> claims =
        [
            new ClaimData(ClaimTypes.Email, user.Email),
            new ClaimData(ClaimTypes.Role, user.Role),
            new ClaimData("tenant_id", user.TenantId.ToString())
        ];
        return Task.FromResult(claims);
    }
}

// 3. Authenticator — implements only VerifyPasswordAsync and GetSubject
public class MyPasswordAuthenticator : PasswordAuthenticatorBase<ApplicationUser>
{
    private readonly IPasswordHasher<ApplicationUser> _hasher;

    public MyPasswordAuthenticator(
        IUserStore<ApplicationUser> userStore,
        IClaimsProvider<ApplicationUser> claimsProvider,
        IPasswordHasher<ApplicationUser> hasher)
        : base(userStore, claimsProvider)
    {
        _hasher = hasher;
    }

    protected override string GetSubject(ApplicationUser user) => user.Id.ToString();

    protected override ValueTask<bool> VerifyPasswordAsync(
        ApplicationUser user, string password, CancellationToken cancellationToken)
    {
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return ValueTask.FromResult(result != PasswordVerificationResult.Failed);
    }

    // Optional: look up by email instead of username
    protected override Task<ApplicationUser?> ResolveUserAsync(
        PasswordSignInRequest request, CancellationToken ct)
        => _userStore.FindByEmailAsync(request.Username, ct);
}

// 4. Registration
builder.Services.AddScoped<IUserStore<ApplicationUser>, UserStore>();
builder.Services.AddScoped<IClaimsProvider<ApplicationUser>, UserClaimsProvider>();
builder.Services.AddScoped<IPasswordAuthenticator, MyPasswordAuthenticator>();
```

---

## API Key flow — `IApiKeyAuthenticator`

```csharp
public class MyApiKeyAuthenticator : IApiKeyAuthenticator
{
    private readonly IApiKeyRepository _repo;

    public MyApiKeyAuthenticator(IApiKeyRepository repo) => _repo = repo;

    public async ValueTask<AuthenticationResult?> AuthenticateAsync(
        ApiKeySignInRequest request,
        CancellationToken cancellationToken = default)
    {
        var keyRecord = await _repo.FindActiveKeyAsync(request.ApiKey, cancellationToken);
        if (keyRecord is null)
            return null;

        return new AuthenticationResult
        {
            Subject = keyRecord.OwnerId.ToString(),
            Claims =
            [
                new ClaimData("api_key_id", keyRecord.Id.ToString()),
                new ClaimData("scope", keyRecord.Scope)
            ]
        };
    }
}
```

### API Key as a direct authentication scheme (without JWT)

To authenticate requests directly with the API key (without issuing a JWT):

```csharp
// IApiKeyValidator — direct validation for the handler
public class MyApiKeyValidator : IApiKeyValidator
{
    private readonly IApiKeyRepository _repo;

    public MyApiKeyValidator(IApiKeyRepository repo) => _repo = repo;

    public async ValueTask<ClaimsPrincipal?> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var keyRecord = await _repo.FindActiveKeyAsync(apiKey, cancellationToken);
        if (keyRecord is null)
            return null;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, keyRecord.OwnerId.ToString()),
            new("api_key_id", keyRecord.Id.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
    }
}

// Registration
builder.Services.AddScoped<IApiKeyValidator, MyApiKeyValidator>();
builder.Services.AddAuthentication()
    .AddTarsIdentityJwtBearer()
    .AddTarsIdentityApiKey(); // ApiKey scheme
```

---

## Magic Link flow

### `IMagicLinkSender` — delivering the link

```csharp
// Via email with SendGrid
public class SendGridMagicLinkSender : IMagicLinkSender
{
    private readonly ISendGridClient _client;
    private readonly IOptions<SendGridOptions> _options;

    public SendGridMagicLinkSender(ISendGridClient client, IOptions<SendGridOptions> options)
    {
        _client = client;
        _options = options;
    }

    public async Task SendAsync(
        string target,
        string linkUrl,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var msg = MailHelper.CreateSingleEmail(
            from: new EmailAddress(_options.Value.FromEmail),
            to: new EmailAddress(target),
            subject: "Your access link",
            plainTextContent: $"Access: {linkUrl} (valid until {expiresAt:g} UTC)",
            htmlContent: $"<a href=\"{linkUrl}\">Click here to sign in</a>");

        await _client.SendEmailAsync(msg, cancellationToken);
    }
}

// Via SMS with Twilio
public class TwilioMagicLinkSender : IMagicLinkSender
{
    public async Task SendAsync(string target, string linkUrl, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        await TwilioClient.GetRestClient().Messages.CreateAsync(
            to: new PhoneNumber(target),
            from: new PhoneNumber("+15551234567"),
            body: $"Your access link (expires at {expiresAt:t}): {linkUrl}");
    }
}
```

### `IMagicLinkIdentityResolver` — mapping payload → user

```csharp
public class MyMagicLinkIdentityResolver : IMagicLinkIdentityResolver
{
    private readonly IUserRepository _repo;

    public MyMagicLinkIdentityResolver(IUserRepository repo) => _repo = repo;

    public async ValueTask<AuthenticationResult?> ResolveAsync(
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken = default)
    {
        // The payload has the data you saved when requesting the magic link
        if (!payload.TryGetValue("target", out var targetObj) || targetObj is not string target)
            return null;

        var user = await _repo.FindByEmailAsync(target, cancellationToken);
        if (user is null)
            return null;

        // First access: you can create the user here
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _repo.SaveChangesAsync(cancellationToken);
        }

        return new AuthenticationResult
        {
            Subject = user.Id.ToString(),
            Claims = [new ClaimData("email", user.Email)]
        };
    }
}
```

---

## Refresh Token — `IRefreshAuthorizationHandler`

Optional. Lets you enrich or block the refresh before issuing the new access token:

```csharp
public class MyRefreshAuthorizationHandler : IRefreshAuthorizationHandler
{
    private readonly IUserRepository _repo;

    public MyRefreshAuthorizationHandler(IUserRepository repo) => _repo = repo;

    public async ValueTask<AuthenticationResult?> AuthorizeAsync(
        string subject,
        IReadOnlyList<ClaimData> claims,
        CancellationToken cancellationToken = default)
    {
        // Re-validates the user — e.g. it may have been disabled since the last login
        var user = await _repo.FindByIdAsync(Guid.Parse(subject), cancellationToken);
        if (user is null || !user.IsActive)
            return null;

        // Returns updated claims (e.g. the role may have changed)
        return new AuthenticationResult
        {
            Subject = subject,
            Claims =
            [
                new ClaimData("email", user.Email),
                new ClaimData("role", user.Role) // updated role
            ],
            SessionVersion = user.SessionVersion
        };
    }
}
```

---

## Sign Out — `ISignOutHandler`

Optional. Called on sign-out for side-effects (auditing, session invalidation, etc.):

```csharp
public class MySignOutHandler : ISignOutHandler
{
    private readonly IAuditLog _audit;
    private readonly ISessionStore _sessions;

    public MySignOutHandler(IAuditLog audit, ISessionStore sessions)
    {
        _audit = audit;
        _sessions = sessions;
    }

    public async Task OnSignOutAsync(string subject, CancellationToken cancellationToken = default)
    {
        await _audit.RecordAsync(subject, "sign_out", cancellationToken);
        await _sessions.InvalidateAllAsync(subject, cancellationToken);
    }
}
```

---

## Issuing tokens manually (outside the endpoints)

If you need to issue tokens in your own code (e.g. after corporate SSO):

```csharp
public class MyCustomLoginService
{
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IRefreshTokenService _refreshService;

    public MyCustomLoginService(ITokenIssuer tokenIssuer, IRefreshTokenService refreshService)
    {
        _tokenIssuer = tokenIssuer;
        _refreshService = refreshService;
    }

    public async Task<(string AccessToken, string? RefreshToken)> LoginAsync(
        string userId,
        IReadOnlyList<ClaimData> claims,
        CancellationToken cancellationToken = default)
    {
        var authResult = new AuthenticationResult
        {
            Subject = userId,
            Claims = claims
        };

        var issued = await _tokenIssuer.IssueAsync(authResult, cancellationToken);

        var refreshResult = await _refreshService.IssueAsync(
            userId, claims, metadata: null, cancellationToken);

        return (issued.AccessToken, refreshResult.OpaqueToken);
    }
}
```

---

## Validating tokens manually

```csharp
public class MyMiddleware
{
    private readonly RequestDelegate _next;

    public MyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITokenValidator validator)
    {
        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");
        if (!string.IsNullOrEmpty(token))
        {
            var principal = await validator.ValidateAsync(token, context.RequestAborted);
            if (principal is not null)
                context.User = principal;
        }
        await _next(context);
    }
}
```
