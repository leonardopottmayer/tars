using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Pottmayer.Tars.Observability.Abstractions;

namespace Pottmayer.Tars.Observability.DI;

/// <summary>
/// Granular registrations for the tars OpenTelemetry pipeline (resource, tracing, metrics and their
/// OTLP exporters). Each method configures a single concern and is safe to call in any order; the
/// underlying <c>AddOpenTelemetry()</c> builder is idempotent. The exporter methods only make sense
/// once their pipeline (<see cref="AddTarsTracing"/> / <see cref="AddTarsMetrics"/>) has been added;
/// see each method's remarks and docs/observability/configuration.md for the canonical order. Logging
/// is registered separately (native or Serilog), and request/HTTP-client instrumentation via the
/// ASP.NET Core package.
/// </summary>
public static class ObservabilityServicesDI
{
    /// <summary>
    /// Sets the resource attributes stamped on every signal (<c>service.name</c> and, optionally,
    /// <c>service.version</c>). Call once; other pipeline registrations build on the same resource.
    /// </summary>
    public static IServiceCollection AddTarsObservabilityResource(
        this IServiceCollection services,
        string serviceName,
        string? serviceVersion = null)
    {
        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion));
        return services;
    }

    /// <summary>
    /// Adds the tracer and subscribes it to every tars <see cref="ActivitySource"/>
    /// (<see cref="TarsTelemetry.Wildcard"/>). Call before <see cref="AddTarsTracingOtlpExporter"/>
    /// and before any tracing instrumentation from the ASP.NET Core package.
    /// </summary>
    public static IServiceCollection AddTarsTracing(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(TarsTelemetry.Wildcard));
        return services;
    }

    /// <summary>
    /// Exports traces over OTLP. Requires <see cref="AddTarsTracing"/> to have been called.
    /// </summary>
    /// <param name="otlpEndpoint">
    /// OTLP endpoint (e.g. <c>http://localhost:4317</c>). When null, the SDK falls back to the
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable and its default.
    /// </param>
    public static IServiceCollection AddTarsTracingOtlpExporter(
        this IServiceCollection services,
        string? otlpEndpoint = null)
    {
        services
            .AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddOtlpExporter(exporter => ApplyEndpoint(exporter, otlpEndpoint)));
        return services;
    }

    /// <summary>
    /// Adds the meter provider and subscribes it to every tars <see cref="System.Diagnostics.Metrics.Meter"/>
    /// (<see cref="TarsTelemetry.Wildcard"/>). Call before <see cref="AddTarsRuntimeMetrics"/>,
    /// <see cref="AddTarsMetricsOtlpExporter"/> and any metrics instrumentation from the ASP.NET Core package.
    /// </summary>
    public static IServiceCollection AddTarsMetrics(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddMeter(TarsTelemetry.Wildcard));
        return services;
    }

    /// <summary>
    /// Adds .NET runtime metrics (GC, heap, thread pool, exceptions). Requires <see cref="AddTarsMetrics"/>.
    /// </summary>
    public static IServiceCollection AddTarsRuntimeMetrics(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddRuntimeInstrumentation());
        return services;
    }

    /// <summary>
    /// Exports metrics over OTLP. Requires <see cref="AddTarsMetrics"/> to have been called.
    /// </summary>
    /// <param name="otlpEndpoint">
    /// OTLP endpoint (e.g. <c>http://localhost:4317</c>). When null, the SDK falls back to the
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable and its default.
    /// </param>
    public static IServiceCollection AddTarsMetricsOtlpExporter(
        this IServiceCollection services,
        string? otlpEndpoint = null)
    {
        services
            .AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddOtlpExporter(exporter => ApplyEndpoint(exporter, otlpEndpoint)));
        return services;
    }

    private static void ApplyEndpoint(OtlpExporterOptions options, string? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint))
            options.Endpoint = new Uri(endpoint);
    }
}
