using System.Security.Claims;

namespace Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

/// <summary>
/// Input passed to each <see cref="ITenantResolver"/> in the pipeline.
/// Carries all context needed to identify the current tenant.
/// HTTP-specific data (headers, host) may be stored in <see cref="Items"/> by the ASP.NET Core middleware.
/// </summary>
public sealed class TenantResolutionRequest
{
    /// <summary>Gets the services available to the resolver.</summary>
    public IServiceProvider Services { get; init; } = default!;
    /// <summary>Gets the tenant key explicitly supplied by the caller.</summary>
    public string? ExplicitTenantKey { get; init; }
    /// <summary>Gets the current principal, when available.</summary>
    public ClaimsPrincipal? Principal { get; init; }
    /// <summary>Gets host-specific data supplied to resolvers.</summary>
    public IReadOnlyDictionary<string, object?> Items { get; init; } =
        new Dictionary<string, object?>();
}
