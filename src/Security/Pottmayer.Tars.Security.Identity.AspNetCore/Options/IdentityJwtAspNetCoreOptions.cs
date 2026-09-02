namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

/// <summary>
/// ASP.NET Core-specific JWT bearer settings.
/// </summary>
public sealed class IdentityJwtAspNetCoreOptions
{
    /// <summary>Whether the JWT bearer handler requires HTTPS metadata. Default: true.</summary>
    public bool RequireHttpsMetadata { get; init; } = true;
}
