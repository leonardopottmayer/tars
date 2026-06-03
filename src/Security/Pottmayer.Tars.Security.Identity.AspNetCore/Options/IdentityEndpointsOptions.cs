namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

public sealed class IdentityEndpointsOptions
{
    public const string DefaultBasePath = "/identity";
    public const string DefaultSignInPasswordPath = "sign-in/password";
    public const string DefaultRequestMagicLinkPath = "sign-in/magic-link/request";
    public const string DefaultConsumeMagicLinkPath = "sign-in/magic-link/consume";
    public const string DefaultSignInApiKeyPath = "sign-in/api-key";
    public const string DefaultRefreshPath = "refresh";
    public const string DefaultSignOutPath = "sign-out";
    public const string DefaultOAuthChallengePath = "sign-in/oauth/{provider}";
    public const string DefaultOAuthCallbackPath = "/identity/callback/oauth";

    public string BasePath { get; init; } = DefaultBasePath;
    public string SignInPasswordPath { get; init; } = DefaultSignInPasswordPath;
    public string RequestMagicLinkPath { get; init; } = DefaultRequestMagicLinkPath;
    public string ConsumeMagicLinkPath { get; init; } = DefaultConsumeMagicLinkPath;
    public string SignInApiKeyPath { get; init; } = DefaultSignInApiKeyPath;
    public string RefreshPath { get; init; } = DefaultRefreshPath;
    public string SignOutPath { get; init; } = DefaultSignOutPath;
    public string OAuthChallengePath { get; init; } = DefaultOAuthChallengePath;
    public string OAuthCallbackPath { get; init; } = DefaultOAuthCallbackPath;
}
