namespace Pottmayer.Tars.Security.Identity.Abstractions.Enums;

/// <summary>
/// Strategy used for stateful token revocation.
/// </summary>
public enum RevocationStrategy
{
    /// <summary>Blacklist by JWT ID (jti); revoked tokens return 401.</summary>
    JtiBlacklist = 0,

    /// <summary>Session version (sv claim); token invalid if sv &lt; current session version.</summary>
    SessionVersion = 1
}
