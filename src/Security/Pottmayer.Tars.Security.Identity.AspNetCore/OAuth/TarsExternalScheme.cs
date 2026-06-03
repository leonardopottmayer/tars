namespace Pottmayer.Tars.Security.Identity.AspNetCore.OAuth;

/// <summary>
/// Constants for the Tars external (OAuth) cookie scheme used to capture provider identities.
/// </summary>
public static class TarsExternalScheme
{
    /// <summary>
    /// The authentication scheme name for the temporary external identity cookie.
    /// Configure as SignInScheme on OAuth providers (e.g. Google, GitHub).
    /// </summary>
    public const string SchemeName = "TarsExternalCookie";
}
