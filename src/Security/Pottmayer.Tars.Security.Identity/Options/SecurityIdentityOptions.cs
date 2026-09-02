namespace Pottmayer.Tars.Security.Identity.Options;

/// <summary>
/// Root options for Tars Identity. Validation in hosting layer.
/// </summary>
public sealed class SecurityIdentityOptions
{
    /// <summary>Default configuration section name for <see cref="SecurityIdentityOptions"/>.</summary>
    public const string SectionName = "Tars:Security:Identity";

    /// <summary>Validation error message used when options validation fails.</summary>
    public const string ValidationErrorMessage = "Invalid SecurityIdentityOptions.";

    /// <summary>JWT issuance and validation settings.</summary>
    public JwtOptions Jwt { get; init; } = new();

    /// <summary>Refresh token settings.</summary>
    public RefreshTokenOptions RefreshToken { get; init; } = new();

    /// <summary>Token delivery mode and hybrid rules.</summary>
    public TokenDeliveryOptions TokenDelivery { get; init; } = new();

    /// <summary>Sign-out and revocation settings.</summary>
    public RevocationOptions Revocation { get; init; } = new();

    /// <summary>Magic link settings.</summary>
    public MagicLinkOptions MagicLink { get; init; } = new();

    /// <summary>Input length limits to protect against DoS attacks on authentication endpoints.</summary>
    public InputLimitsOptions InputLimits { get; init; } = new();
}
