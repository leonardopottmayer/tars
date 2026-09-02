namespace Pottmayer.Tars.Multitenancy.Abstractions.Context;

/// <summary>
/// Represents the resolved tenant for the current execution scope.
/// Available in HTTP requests, hosted services, jobs, tests and CLI.
/// </summary>
public interface ITenantContext
{
    /// <summary>Gets whether a tenant was resolved.</summary>
    bool IsResolved { get; }
    /// <summary>Gets the technical tenant identifier.</summary>
    string? TenantKey { get; }
    /// <summary>Gets the optional tenant code.</summary>
    string? TenantCode { get; }
    /// <summary>Gets additional tenant data.</summary>
    IReadOnlyDictionary<string, object?> Properties { get; }
}
