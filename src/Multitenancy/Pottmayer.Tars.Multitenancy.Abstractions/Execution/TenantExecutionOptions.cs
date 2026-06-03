namespace Pottmayer.Tars.Multitenancy.Abstractions.Execution;

/// <summary>
/// Options for running work across multiple tenants.
/// </summary>
public sealed class TenantExecutionOptions
{
    /// <summary>
    /// Maximum number of tenants to process concurrently.
    /// Defaults to 1 (sequential). Increase for parallelism.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 1;
}
