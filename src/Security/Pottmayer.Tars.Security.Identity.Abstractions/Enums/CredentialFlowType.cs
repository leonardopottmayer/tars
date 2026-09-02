namespace Pottmayer.Tars.Security.Identity.Abstractions.Enums;

/// <summary>
/// Supported credential flow types for authentication.
/// </summary>
public enum CredentialFlowType
{
    /// <summary>Sign-in with a username/password credential.</summary>
    Password = 0,

    /// <summary>Sign-in via a magic link token.</summary>
    MagicLink = 1,

    /// <summary>Sign-in via an external OAuth provider.</summary>
    OAuth = 2,

    /// <summary>Sign-in with an API key.</summary>
    ApiKey = 3,

    /// <summary>Re-authentication via a refresh token.</summary>
    RefreshToken = 4
}
