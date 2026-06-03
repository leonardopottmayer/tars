namespace Pottmayer.Tars.Security.Identity.Options;

/// <summary>
/// JWT issuance and validation options.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Issuer (iss) claim.</summary>
    public string Issuer { get; init; } = "tars";

    /// <summary>Audience (aud) claim.</summary>
    public string Audience { get; init; } = "tars";

    /// <summary>Signing key (base64 or raw; symmetric). For RS256 use separate key configuration.</summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>Access token lifetime.</summary>
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>Clock skew allowed when validating expiration.</summary>
    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromMinutes(1);
}
