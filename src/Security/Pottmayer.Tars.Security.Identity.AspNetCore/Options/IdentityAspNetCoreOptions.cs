using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

/// <summary>
/// Root ASP.NET Core-specific options for Tars Identity. Binds from the same configuration
/// section as <see cref="SecurityIdentityOptions"/>; validation happens independently.
/// </summary>
public sealed class IdentityAspNetCoreOptions
{
    /// <summary>Default configuration section name, shared with <see cref="SecurityIdentityOptions"/>.</summary>
    public const string SectionName = SecurityIdentityOptions.SectionName;

    /// <summary>Validation error message used when options validation fails.</summary>
    public const string ValidationErrorMessage = "Invalid IdentityAspNetCoreOptions.";

    /// <summary>JWT bearer settings.</summary>
    public IdentityJwtAspNetCoreOptions Jwt { get; init; } = new();

    /// <summary>Auth cookie settings.</summary>
    public IdentityCookieOptions Cookie { get; init; } = new();

    /// <summary>Refresh token transport settings.</summary>
    public RefreshTokenAspNetCoreOptions RefreshToken { get; init; } = new();

    /// <summary>Token delivery settings for hybrid mode.</summary>
    public TokenDeliveryAspNetCoreOptions TokenDelivery { get; init; } = new();

    /// <summary>API key authentication settings.</summary>
    public ApiKeyOptions ApiKey { get; init; } = new();

    /// <summary>Minimal API endpoint route settings.</summary>
    public IdentityEndpointsOptions Endpoints { get; init; } = new();

    /// <summary>Magic link settings.</summary>
    public MagicLinkAspNetCoreOptions MagicLink { get; init; } = new();
}
