namespace Pottmayer.Tars.Multitenancy.Abstractions.Context;

/// <summary>
/// Represents the resolved tenant for the current execution scope.
/// Available in HTTP requests, hosted services, jobs, tests and CLI.
/// </summary>
public interface ITenantContext
{
    bool IsResolved { get; }
    string? TenantKey { get; }
    string? TenantCode { get; }
    IReadOnlyDictionary<string, object?> Properties { get; }
}
