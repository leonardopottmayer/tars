using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

public sealed class IdentityAspNetCoreOptions
{
    public const string SectionName = IdentityOptions.SectionName;
    public const string ValidationErrorMessage = "Invalid IdentityAspNetCoreOptions.";

    public IdentityJwtAspNetCoreOptions Jwt { get; init; } = new();
    public IdentityCookieOptions Cookie { get; init; } = new();
    public RefreshTokenAspNetCoreOptions RefreshToken { get; init; } = new();
    public TokenDeliveryAspNetCoreOptions TokenDelivery { get; init; } = new();
    public ApiKeyOptions ApiKey { get; init; } = new();
    public IdentityEndpointsOptions Endpoints { get; init; } = new();
    public MagicLinkAspNetCoreOptions MagicLink { get; init; } = new();
}
