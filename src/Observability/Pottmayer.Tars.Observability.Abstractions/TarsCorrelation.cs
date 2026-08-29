namespace Pottmayer.Tars.Observability.Abstractions;

/// <summary>Conventions for correlation-id propagation and logging.</summary>
public static class TarsCorrelation
{
    /// <summary>HTTP header that carries the correlation id in and out of a service.</summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>Log property and span-tag key under which the correlation id is recorded.</summary>
    public const string PropertyName = "CorrelationId";
}
