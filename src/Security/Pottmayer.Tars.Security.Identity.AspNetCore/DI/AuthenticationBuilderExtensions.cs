using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.AspNetCore.Authentication;
using Pottmayer.Tars.Security.Identity.AspNetCore.OAuth;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.DI;

public static class AuthenticationBuilderExtensions
{
    public const string DefaultApiKeyScheme = "ApiKey";

    public static AuthenticationBuilder AddTarsIdentityApiKey(this AuthenticationBuilder builder, string schemeName = DefaultApiKeyScheme)
    {
        builder.Services
            .AddOptions<ApiKeyAuthenticationOptions>(schemeName)
            .Configure<IOptionsMonitor<IdentityAspNetCoreOptions>>((options, aspNetCoreOptionsMonitor) =>
            {
                var apiKeyOptions = aspNetCoreOptionsMonitor.CurrentValue.ApiKey;
                options.HeaderName = apiKeyOptions.HeaderName;
                options.QueryParameterName = apiKeyOptions.QueryParameterName;
            });

        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(schemeName, _ => { });
    }

    /// <summary>
    /// Registers a short-lived cookie scheme (<see cref="TarsExternalScheme.SchemeName"/>) used to
    /// capture the external provider identity between the OAuth redirect and the callback endpoint.
    /// Configure your OAuth providers with <c>SignInScheme = TarsExternalScheme.SchemeName</c>.
    /// </summary>
    public static AuthenticationBuilder AddTarsIdentityExternalScheme(
        this AuthenticationBuilder builder,
        string? callbackPath = null)
    {
        builder.AddCookie(TarsExternalScheme.SchemeName, options =>
        {
            options.Cookie.Name = TarsExternalScheme.SchemeName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            options.SlidingExpiration = false;

            if (!string.IsNullOrEmpty(callbackPath))
                options.LoginPath = callbackPath;
        });

        return builder;
    }
}
