using System.Security.Claims;

namespace Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

/// <summary>
/// Input passed to each <see cref="ITenantResolver"/> in the pipeline.
/// Carries all context needed to identify the current tenant.
/// HTTP-specific data (headers, host) may be stored in <see cref="Items"/> by the ASP.NET Core middleware.
/// </summary>
public sealed class TenantResolutionRequest
{
    public IServiceProvider Services { get; init; } = default!;
    public string? ExplicitTenantKey { get; init; }
    public ClaimsPrincipal? Principal { get; init; }
    public IReadOnlyDictionary<string, object?> Items { get; init; } =
        new Dictionary<string, object?>();
}
