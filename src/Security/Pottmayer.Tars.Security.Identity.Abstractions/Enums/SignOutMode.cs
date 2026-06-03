namespace Pottmayer.Tars.Security.Identity.Abstractions.Enums;

/// <summary>
/// Defines how sign-out revokes tokens.
/// </summary>
public enum SignOutMode
{
    /// <summary>Only refresh token is revoked; access token remains valid until expiration.</summary>
    Stateless = 0,

    /// <summary>Both refresh token and access token are revoked (requires revocation store).</summary>
    Stateful = 1
}
