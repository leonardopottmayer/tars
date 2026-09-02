namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

/// <summary>
/// Route paths for the built-in Identity minimal API endpoints.
/// </summary>
public sealed class IdentityEndpointsOptions
{
    /// <summary>Default base path all endpoint paths below are relative to.</summary>
    public const string DefaultBasePath = "/identity";

    /// <summary>Default path for password sign-in.</summary>
    public const string DefaultSignInPasswordPath = "sign-in/password";

    /// <summary>Default path for requesting a magic link.</summary>
    public const string DefaultRequestMagicLinkPath = "sign-in/magic-link/request";

    /// <summary>Default path for consuming a magic link token.</summary>
    public const string DefaultConsumeMagicLinkPath = "sign-in/magic-link/consume";

    /// <summary>Default path for API key sign-in.</summary>
    public const string DefaultSignInApiKeyPath = "sign-in/api-key";

    /// <summary>Default path for refreshing tokens.</summary>
    public const string DefaultRefreshPath = "refresh";

    /// <summary>Default path for sign-out.</summary>
    public const string DefaultSignOutPath = "sign-out";

    /// <summary>Default path for starting an OAuth challenge; <c>{provider}</c> is a route parameter.</summary>
    public const string DefaultOAuthChallengePath = "sign-in/oauth/{provider}";

    /// <summary>Default OAuth callback path (absolute; not relative to <see cref="BasePath"/>).</summary>
    public const string DefaultOAuthCallbackPath = "/identity/callback/oauth";

    /// <summary>Base path all relative endpoint paths below are mapped under.</summary>
    public string BasePath { get; init; } = DefaultBasePath;

    /// <summary>Path for password sign-in.</summary>
    public string SignInPasswordPath { get; init; } = DefaultSignInPasswordPath;

    /// <summary>Path for requesting a magic link.</summary>
    public string RequestMagicLinkPath { get; init; } = DefaultRequestMagicLinkPath;

    /// <summary>Path for consuming a magic link token.</summary>
    public string ConsumeMagicLinkPath { get; init; } = DefaultConsumeMagicLinkPath;

    /// <summary>Path for API key sign-in.</summary>
    public string SignInApiKeyPath { get; init; } = DefaultSignInApiKeyPath;

    /// <summary>Path for refreshing tokens.</summary>
    public string RefreshPath { get; init; } = DefaultRefreshPath;

    /// <summary>Path for sign-out.</summary>
    public string SignOutPath { get; init; } = DefaultSignOutPath;

    /// <summary>Path for starting an OAuth challenge.</summary>
    public string OAuthChallengePath { get; init; } = DefaultOAuthChallengePath;

    /// <summary>OAuth callback path.</summary>
    public string OAuthCallbackPath { get; init; } = DefaultOAuthCallbackPath;
}
