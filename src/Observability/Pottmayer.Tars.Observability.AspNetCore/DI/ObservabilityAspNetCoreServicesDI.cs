using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Pottmayer.Tars.Observability.AspNetCore.Middleware;

namespace Pottmayer.Tars.Observability.AspNetCore.DI;

/// <summary>
/// ASP.NET Core instrumentation for the tars observability pipeline and the correlation-id
/// middleware. The instrumentation methods extend the pipelines registered in the core package, so
/// call the tracing ones after <c>AddTarsTracing</c> and the metrics ones after <c>AddTarsMetrics</c>;
/// see docs/observability/configuration.md for the canonical order.
/// </summary>
public static class ObservabilityAspNetCoreServicesDI
{
    /// <summary>
    /// Adds ASP.NET Core request instrumentation to the tracer (inbound-request spans).
    /// Requires <c>AddTarsTracing</c>.
    /// </summary>
    public static IServiceCollection AddTarsAspNetCoreTracing(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation());
        return services;
    }

    /// <summary>
    /// Adds outbound HttpClient instrumentation to the tracer (dependency-call spans).
    /// Requires <c>AddTarsTracing</c>.
    /// </summary>
    public static IServiceCollection AddTarsHttpClientTracing(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddHttpClientInstrumentation());
        return services;
    }

    /// <summary>
    /// Adds ASP.NET Core request instrumentation to metrics (request duration/count).
    /// Requires <c>AddTarsMetrics</c>.
    /// </summary>
    public static IServiceCollection AddTarsAspNetCoreMetrics(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());
        return services;
    }

    /// <summary>
    /// Adds outbound HttpClient instrumentation to metrics (dependency-call duration/count).
    /// Requires <c>AddTarsMetrics</c>.
    /// </summary>
    public static IServiceCollection AddTarsHttpClientMetrics(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddHttpClientInstrumentation());
        return services;
    }

    /// <summary>
    /// Adds the correlation-id middleware. Place it early in the pipeline so downstream logs and
    /// spans inherit the id.
    /// </summary>
    public static IApplicationBuilder UseTarsCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
