using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Contracts;
using Pottmayer.Tars.Security.Identity.Abstractions.Dtos;
using Pottmayer.Tars.Security.Identity.Abstractions.Enums;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Abstractions.Services;
using Pottmayer.Tars.Security.Identity.Abstractions.Token;
using Pottmayer.Tars.Security.Identity.Abstractions.TokenDelivery;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.OAuth;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;
using Pottmayer.Tars.Security.Identity.AspNetCore.ProblemDetails;
using Pottmayer.Tars.Security.Identity.AspNetCore.Token;
using Pottmayer.Tars.Security.Identity.Options;
using Pottmayer.Tars.Security.Identity.TokenDelivery;
using System.Security.Claims;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Endpoints;

public static class IdentityEndpoints
{
    public const string DefaultBasePath = IdentityEndpointsOptions.DefaultBasePath;

    private static IdentityEndpointsOptions GetEndpointsOptions(IEndpointRouteBuilder app)
    {
        var optionsMonitor = app.ServiceProvider.GetRequiredService<IOptionsMonitor<IdentityAspNetCoreOptions>>();
        return optionsMonitor.CurrentValue.Endpoints;
    }

    /// <summary>
    /// Maps all identity endpoints. OAuth endpoints are included automatically if
    /// <see cref="IOAuthUserLinker"/> is registered in the container.
    /// </summary>
    public static IEndpointRouteBuilder MapTarsIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapTarsIdentitySignInPasswordEndpoint();
        app.MapTarsIdentityRequestMagicLinkEndpoint();
        app.MapTarsIdentityConsumeMagicLinkEndpoint();
        app.MapTarsIdentitySignInApiKeyEndpoint();
        app.MapTarsIdentityRefreshEndpoint();
        app.MapTarsIdentitySignOutEndpoint();

        var oauthLinker = app.ServiceProvider.GetService<IOAuthUserLinker>();
        if (oauthLinker is not null)
        {
            app.MapTarsIdentityOAuthChallengeEndpoint();
            app.MapTarsIdentityOAuthCallbackEndpoint();
        }

        return app;
    }

    public static IEndpointRouteBuilder MapTarsIdentitySignInPasswordEndpoint(this IEndpointRouteBuilder app)
    {
        var e = GetEndpointsOptions(app);
        app.MapGroup(e.BasePath)
            .MapPost(e.SignInPasswordPath, SignInPassword)
            .WithName("TarsIdentity_SignInPassword")
            .AllowAnonymous();
        return app;
    }

    public static IEndpointRouteBuilder MapTarsIdentityRequestMagicLinkEndpoint(this IEndpointRouteBuilder app)
    {
        var e = GetEndpointsOptions(app);
        app.MapGroup(e.BasePath)
            .MapPost(e.RequestMagicLinkPath, RequestMagicLink)
            .WithName("TarsIdentity_RequestMagicLink")
            .AllowAnonymous();
        return app;
    }

    public static IEndpointRouteBuilder MapTarsIdentityConsumeMagicLinkEndpoint(this IEndpointRouteBuilder app)
    {
        var e = GetEndpointsOptions(app);
        app.MapGroup(e.BasePath)
            .MapPost(e.ConsumeMagicLinkPath, ConsumeMagicLink)
            .WithName("TarsIdentity_ConsumeMagicLink")
            .AllowAnonymous();
        return app;
    }

    public static IEndpointRouteBuilder MapTarsIdentitySignInApiKeyEndpoint(this IEndpointRouteBuilder app)
    {
        var e = GetEndpointsOptions(app);
        app.MapGroup(e.BasePath)
            .MapPost(e.SignInApiKeyPath, SignInApiKey)
            .WithName("TarsIdentity_SignInApiKey")
            .AllowAnonymous();
        return app;
    }

    public static IEndpointRouteBuilder MapTarsIdentityRefreshEndpoint(this IEndpointRouteBuilder app)
    {
        var e = GetEndpointsOptions(app);
        app.MapGroup(e.BasePath)
            .MapPost(e.RefreshPath, Refresh)
            .WithName("TarsIdentity_Refresh")
            .AllowAnonymous();
        return app;
    }

    public static IEndpointRouteBuilder MapTarsIdentitySignOutEndpoint(this IEndpointRouteBuilder app)
    {
        var e = GetEndpointsOptions(app);
        app.MapGroup(e.BasePath)
            .MapPost(e.SignOutPath, SignOut)
            .WithName("TarsIdentity_SignOut")
            .RequireAuthorization();
        return app;
    }

    public static IEndpointRouteBuilder MapTarsIdentityOAuthChallengeEndpoint(this IEndpointRouteBuilder app)
    {
        var e = GetEndpointsOptions(app);
        app.MapGroup(e.BasePath)
            .MapGet(e.OAuthChallengePath, OAuthChallenge)
            .WithName("TarsIdentity_OAuthChallenge")
            .AllowAnonymous();
        return app;
    }

    public static IEndpointRouteBuilder MapTarsIdentityOAuthCallbackEndpoint(this IEndpointRouteBuilder app)
    {
        var e = GetEndpointsOptions(app);
        app.MapGet(e.OAuthCallbackPath, OAuthCallback)
            .WithName("TarsIdentity_OAuthCallback")
            .AllowAnonymous();
        return app;
    }

    private static IResult OAuthChallenge(
        string provider,
        HttpContext context,
        [FromQuery] string? returnUrl)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = IdentityEndpointsOptions.DefaultOAuthCallbackPath,
            Items = { ["provider"] = provider }
        };

        if (!string.IsNullOrEmpty(returnUrl))
            properties.Items["returnUrl"] = returnUrl;

        return Results.Challenge(properties, [provider]);
    }

    private static async Task<IResult> OAuthCallback(
        HttpContext context,
        [FromServices] IOAuthUserLinker linker,
        [FromServices] ITokenIssuer jwtIssuer,
        [FromServices] IRefreshTokenService refreshService,
        [FromServices] ITokenOutputWriter outputWriter,
        [FromServices] TokenDeliveryPolicy policy,
        [FromServices] IOptionsMonitor<IdentityOptions> optionsMonitor,
        [FromServices] IOptionsMonitor<IdentityAspNetCoreOptions> aspNetCoreOptionsMonitor,
        CancellationToken cancellationToken)
    {
        var result = await context.AuthenticateAsync(TarsExternalScheme.SchemeName).ConfigureAwait(false);
        if (!result.Succeeded || result.Principal is null)
            return Results.Unauthorized();

        await context.SignOutAsync(TarsExternalScheme.SchemeName).ConfigureAwait(false);

        var provider = result.Properties?.Items.TryGetValue("provider", out var p) == true ? p ?? "unknown" : "unknown";
        var externalId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        var externalClaims = result.Principal.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => (string?)g.First().Value);

        var authResult = await linker.LinkAsync(provider, externalClaims, externalId, cancellationToken).ConfigureAwait(false);
        if (authResult is null)
            return Results.Unauthorized();

        return await IssueTokensAsync(
            context,
            authResult,
            jwtIssuer,
            refreshService,
            outputWriter,
            policy,
            optionsMonitor.CurrentValue,
            aspNetCoreOptionsMonitor.CurrentValue,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IResult> SignInPassword(
        [FromBody] PasswordSignInRequest? request,
        HttpContext context,
        [FromServices] IPasswordAuthenticator authenticator,
        [FromServices] ITokenIssuer jwtIssuer,
        [FromServices] IRefreshTokenService refreshService,
        [FromServices] ITokenOutputWriter outputWriter,
        [FromServices] TokenDeliveryPolicy policy,
        [FromServices] IOptionsMonitor<IdentityOptions> optionsMonitor,
        [FromServices] IOptionsMonitor<IdentityAspNetCoreOptions> aspNetCoreOptionsMonitor,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(IdentityProblemDetails.Validation("Invalid request.", "UserNameOrEmail and Password are required."));

        var limits = optionsMonitor.CurrentValue.InputLimits;
        if (limits.MaxUsernameLength > 0 && request.Username.Length > limits.MaxUsernameLength)
            return Results.BadRequest(IdentityProblemDetails.Validation("Invalid request.", $"Username cannot exceed {limits.MaxUsernameLength} characters."));
        if (limits.MaxPasswordLength > 0 && request.Password.Length > limits.MaxPasswordLength)
            return Results.BadRequest(IdentityProblemDetails.Validation("Invalid request.", $"Password cannot exceed {limits.MaxPasswordLength} characters."));

        var result = await authenticator.AuthenticateAsync(request, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return Results.Unauthorized();

        return await IssueTokensAsync(
            context,
            result,
            jwtIssuer,
            refreshService,
            outputWriter,
            policy,
            optionsMonitor.CurrentValue,
            aspNetCoreOptionsMonitor.CurrentValue,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IResult> RequestMagicLink(
        [FromBody] MagicLinkRequestRequest? request,
        [FromServices] IMagicLinkSender sender,
        [FromServices] IMagicLinkTokenService magicLinkService,
        [FromServices] IOptionsMonitor<IdentityOptions> optionsMonitor,
        [FromServices] IOptionsMonitor<IdentityAspNetCoreOptions> aspNetCoreOptionsMonitor,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Target))
            return Results.BadRequest(IdentityProblemDetails.Validation("Invalid request.", "Target is required."));

        var limits = optionsMonitor.CurrentValue.InputLimits;
        if (limits.MaxMagicLinkTargetLength > 0 && request.Target.Length > limits.MaxMagicLinkTargetLength)
            return Results.BadRequest(IdentityProblemDetails.Validation("Invalid request.", $"Target cannot exceed {limits.MaxMagicLinkTargetLength} characters."));
        if (!string.IsNullOrEmpty(request.ReturnUrl) && limits.MaxReturnUrlLength > 0 && request.ReturnUrl.Length > limits.MaxReturnUrlLength)
            return Results.BadRequest(IdentityProblemDetails.Validation("Invalid request.", $"ReturnUrl cannot exceed {limits.MaxReturnUrlLength} characters."));

        var payload = new Dictionary<string, object?> { ["target"] = request.Target };
        if (!string.IsNullOrEmpty(request.ReturnUrl))
            payload["returnUrl"] = request.ReturnUrl;

        var issueResult = await magicLinkService.IssueAsync(payload, cancellationToken).ConfigureAwait(false);
        var linkUrl = BuildMagicLinkUrl(aspNetCoreOptionsMonitor.CurrentValue.MagicLink, issueResult.Token);
        await sender.SendAsync(request.Target, linkUrl, issueResult.ExpiresAt, cancellationToken).ConfigureAwait(false);

        return Results.Ok(new { message = "Magic link sent.", expiresAt = issueResult.ExpiresAt });
    }

    private static async Task<IResult> ConsumeMagicLink(
        [FromBody] MagicLinkConsumeRequest? body,
        [FromQuery] string? token,
        HttpContext context,
        [FromServices] IMagicLinkIdentityResolver resolver,
        [FromServices] IMagicLinkTokenService magicLinkService,
        [FromServices] ITokenIssuer jwtIssuer,
        [FromServices] IRefreshTokenService refreshService,
        [FromServices] ITokenOutputWriter outputWriter,
        [FromServices] TokenDeliveryPolicy policy,
        [FromServices] IOptionsMonitor<IdentityOptions> optionsMonitor,
        [FromServices] IOptionsMonitor<IdentityAspNetCoreOptions> aspNetCoreOptionsMonitor,
        CancellationToken cancellationToken)
    {
        var tokenValue = body?.Token ?? token;
        if (string.IsNullOrWhiteSpace(tokenValue))
            return Results.BadRequest(IdentityProblemDetails.Validation("Invalid request.", "Token is required."));

        var payload = await magicLinkService.ConsumeAsync(tokenValue, cancellationToken).ConfigureAwait(false);
        if (payload is null)
            return Results.Unauthorized();

        var result = await resolver.ResolveAsync(payload, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return Results.Unauthorized();

        return await IssueTokensAsync(
            context,
            result,
            jwtIssuer,
            refreshService,
            outputWriter,
            policy,
            optionsMonitor.CurrentValue,
            aspNetCoreOptionsMonitor.CurrentValue,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IResult> SignInApiKey(
        HttpContext context,
        [FromServices] IApiKeyAuthenticator authenticator,
        [FromServices] ITokenIssuer jwtIssuer,
        [FromServices] IRefreshTokenService refreshService,
        [FromServices] ITokenOutputWriter outputWriter,
        [FromServices] TokenDeliveryPolicy policy,
        [FromServices] IOptionsMonitor<IdentityOptions> optionsMonitor,
        [FromServices] IOptionsMonitor<IdentityAspNetCoreOptions> aspNetCoreOptionsMonitor,
        CancellationToken cancellationToken)
    {
        var apiKeyHeaderName = aspNetCoreOptionsMonitor.CurrentValue.ApiKey.HeaderName;
        var apiKey = context.Request.Headers[apiKeyHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Results.BadRequest(IdentityProblemDetails.Validation("Invalid request.", $"{apiKeyHeaderName} header is required."));

        var limits = optionsMonitor.CurrentValue.InputLimits;
        if (limits.MaxApiKeyLength > 0 && apiKey.Length > limits.MaxApiKeyLength)
            return Results.BadRequest(IdentityProblemDetails.Validation("Invalid request.", $"API key cannot exceed {limits.MaxApiKeyLength} characters."));

        var result = await authenticator.AuthenticateAsync(new ApiKeySignInRequest { ApiKey = apiKey }, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return Results.Unauthorized();

        return await IssueTokensAsync(
            context,
            result,
            jwtIssuer,
            refreshService,
            outputWriter,
            policy,
            optionsMonitor.CurrentValue,
            aspNetCoreOptionsMonitor.CurrentValue,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IResult> Refresh(
        HttpContext context,
        [FromServices] IRefreshAuthorizationHandler? refreshHandler,
        [FromServices] IRefreshTokenService refreshService,
        [FromServices] ITokenIssuer jwtIssuer,
        [FromServices] ITokenInputReader reader,
        [FromServices] ITokenOutputWriter outputWriter,
        [FromServices] TokenDeliveryPolicy policy,
        [FromServices] IOptionsMonitor<IdentityOptions> optionsMonitor,
        [FromServices] IOptionsMonitor<IdentityAspNetCoreOptions> aspNetCoreOptionsMonitor,
        CancellationToken cancellationToken)
    {
        var readContext = HttpContextTokenBridge.CreateReadContext(context);
        var refreshToken = reader.ReadRefreshToken(readContext);
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Results.BadRequest(IdentityProblemDetails.Validation("Invalid request.", "Refresh token is required."));

        var consumeResult = await refreshService.ConsumeAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        if (consumeResult is null)
            return Results.Unauthorized();

        AuthenticationResult authResult;
        if (refreshHandler is not null)
        {
            var auth = await refreshHandler.AuthorizeAsync(consumeResult.Payload.Subject, consumeResult.Payload.Claims, cancellationToken).ConfigureAwait(false);
            if (auth is null)
                return Results.Unauthorized();
            authResult = auth;
        }
        else
        {
            authResult = new AuthenticationResult
            {
                Subject = consumeResult.Payload.Subject,
                Claims = consumeResult.Payload.Claims,
                SessionVersion = null
            };
        }

        return await IssueTokensAsync(
            context,
            authResult,
            jwtIssuer,
            refreshService,
            outputWriter,
            policy,
            optionsMonitor.CurrentValue,
            aspNetCoreOptionsMonitor.CurrentValue,
            consumeResult.ShouldIssueNewRefreshToken,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IResult> SignOut(
        HttpContext context,
        [FromServices] IRefreshTokenService refreshService,
        [FromServices] ITokenRevocationService? revocationService,
        [FromServices] ISignOutHandler? signOutHandler,
        [FromServices] ITokenInputReader reader,
        [FromServices] IOptionsMonitor<IdentityOptions> optionsMonitor,
        [FromServices] IOptionsMonitor<IdentityAspNetCoreOptions> aspNetCoreOptionsMonitor,
        CancellationToken cancellationToken)
    {
        var options = optionsMonitor.CurrentValue;
        var aspNetCoreOptions = aspNetCoreOptionsMonitor.CurrentValue;
        var subject = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subject))
        {
            if (signOutHandler is not null)
                await signOutHandler.OnSignOutAsync(subject, cancellationToken).ConfigureAwait(false);

            var readContext = HttpContextTokenBridge.CreateReadContext(context);
            var refreshToken = reader.ReadRefreshToken(readContext);
            if (!string.IsNullOrEmpty(refreshToken))
                await refreshService.RevokeAsync(refreshToken, cancellationToken).ConfigureAwait(false);

            if (options.Revocation?.SignOutMode == SignOutMode.Stateful && options.Revocation.StatefulRevocationEnabled && revocationService is not null)
            {
                var jti = context.User.FindFirst("jti")?.Value;
                if (!string.IsNullOrEmpty(jti))
                {
                    var exp = context.User.FindFirst("exp")?.Value;
                    if (!string.IsNullOrEmpty(exp) && long.TryParse(exp, out var expUnix))
                        await revocationService.RevokeJtiAsync(jti, DateTimeOffset.FromUnixTimeSeconds(expUnix), cancellationToken).ConfigureAwait(false);
                }

                if (options.Revocation.Strategy == RevocationStrategy.SessionVersion)
                    await revocationService.IncrementSessionVersionAsync(subject, cancellationToken).ConfigureAwait(false);
            }
        }

        if (options.TokenDelivery.Mode == TokenDeliveryMode.CookieOnly || options.TokenDelivery.Mode == TokenDeliveryMode.Hybrid)
            HttpContextTokenBridge.DeleteAuthCookies(context, aspNetCoreOptions);

        return Results.Ok(new { message = "Signed out." });
    }

    private static async Task<IResult> IssueTokensAsync(
        HttpContext context,
        AuthenticationResult result,
        ITokenIssuer jwtIssuer,
        IRefreshTokenService refreshService,
        ITokenOutputWriter outputWriter,
        TokenDeliveryPolicy policy,
        IdentityOptions options,
        IdentityAspNetCoreOptions aspNetCoreOptions,
        bool issueRefreshToken = true,
        CancellationToken cancellationToken = default)
    {
        var issued = await jwtIssuer.IssueAsync(result, cancellationToken).ConfigureAwait(false);

        string? refreshToken = null;
        if (options.RefreshToken.Enabled && issueRefreshToken)
        {
            var refreshResult = await refreshService.IssueAsync(result.Subject, result.Claims, null, cancellationToken).ConfigureAwait(false);
            refreshToken = refreshResult.OpaqueToken;
        }

        var tokenResponse = new TokenResponse
        {
            AccessToken = issued.AccessToken,
            RefreshToken = refreshToken,
            ExpiresAt = issued.ExpiresAt,
            TokenType = "Bearer"
        };

        var deliveryContext = new TokenDeliveryContext
        {
            HasAuthorizationHeader = !string.IsNullOrEmpty(context.Request.Headers.Authorization.FirstOrDefault()),
            HasAuthCookie = context.Request.Cookies.ContainsKey(aspNetCoreOptions.Cookie.AccessTokenCookieName),
            ClientTypeHeaderValue = context.Request.Headers[aspNetCoreOptions.TokenDelivery.HybridClientTypeHeader].FirstOrDefault(),
            ConfiguredMode = options.TokenDelivery.Mode
        };

        var effectiveMode = TokenDeliveryAspNetCoreResolver.ResolveEffectiveMode(
            options,
            aspNetCoreOptions,
            policy,
            deliveryContext.HasAuthorizationHeader,
            deliveryContext.HasAuthCookie,
            deliveryContext.ClientTypeHeaderValue);

        var writeContext = new TokenWriteContext();
        await outputWriter.WriteAsync(writeContext, tokenResponse, effectiveMode, cancellationToken).ConfigureAwait(false);
        HttpContextTokenBridge.ApplyWriteContext(context, writeContext);

        if (writeContext.Body is not null)
            return Results.Ok(writeContext.Body);

        return Results.Ok(new { message = "Signed in.", expiresAt = issued.ExpiresAt });
    }

    private static string BuildMagicLinkUrl(MagicLinkAspNetCoreOptions options, string token)
    {
        var separator = options.BaseUrl.Contains('?') ? "&" : "?";
        return $"{options.BaseUrl.TrimEnd('/')}{separator}{options.TokenQueryParameter}={Uri.EscapeDataString(token)}";
    }
}
