using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Pottmayer.Tars.Observability.AspNetCore.DI;
using Pottmayer.Tars.Observability.DI;

namespace Pottmayer.Tars.Observability.Tests.Unit;

public class ObservabilityTracingDITests
{
    [Fact]
    public void AddTarsTracing_registers_a_tracer_provider()
    {
        var services = new ServiceCollection();

        services.AddTarsObservabilityResource("pandora");
        services.AddTarsTracing();

        using var provider = services.BuildServiceProvider();
        provider.GetService<TracerProvider>().Should().NotBeNull();
    }

    [Fact]
    public void Tracing_with_instrumentation_and_the_otlp_exporter_composes()
    {
        var services = new ServiceCollection();

        services.AddTarsTracing();
        services.AddTarsAspNetCoreTracing();
        services.AddTarsHttpClientTracing();
        services.AddTarsTracingOtlpExporter("http://collector:4317");

        using var provider = services.BuildServiceProvider();
        provider.GetService<TracerProvider>().Should().NotBeNull();
    }
}

public class ObservabilityMetricsDITests
{
    [Fact]
    public void AddTarsMetrics_registers_a_meter_provider()
    {
        var services = new ServiceCollection();

        services.AddTarsMetrics();

        using var provider = services.BuildServiceProvider();
        provider.GetService<MeterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void Metrics_with_runtime_instrumentation_and_the_otlp_exporter_composes()
    {
        var services = new ServiceCollection();

        services.AddTarsMetrics();
        services.AddTarsAspNetCoreMetrics();
        services.AddTarsHttpClientMetrics();
        services.AddTarsRuntimeMetrics();
        services.AddTarsMetricsOtlpExporter("http://collector:4317");

        using var provider = services.BuildServiceProvider();
        provider.GetService<MeterProvider>().Should().NotBeNull();
    }
}

public class ObservabilityLoggingDITests
{
    [Fact]
    public void AddTarsLogging_registers_a_logger_provider()
    {
        var services = new ServiceCollection();

        services.AddTarsLogging();

        using var provider = services.BuildServiceProvider();
        provider.GetService<LoggerProvider>().Should().NotBeNull();
    }

    [Fact]
    public void Logging_with_the_otlp_exporter_composes()
    {
        var services = new ServiceCollection();

        services.AddTarsLogging();
        services.AddTarsLoggingOtlpExporter("http://collector:4317");

        using var provider = services.BuildServiceProvider();
        provider.GetService<LoggerProvider>().Should().NotBeNull();
    }
}
