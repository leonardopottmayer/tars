using global::Serilog;
using global::Serilog.Sinks.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Pottmayer.Tars.Observability.Serilog.DI;

/// <summary>
/// Pluggable Serilog logging provider for tars observability. Use this instead of the core
/// <c>AddTarsLogging</c> when you want Serilog as the logging front-end. Serilog becomes the sole
/// logging provider, so the sinks chosen here replace the default logging providers.
/// <see cref="AddTarsSerilog"/> always enables <c>Enrich.FromLogContext()</c>, which lets the
/// correlation-id middleware's <c>ILogger.BeginScope</c> properties flow into every log line.
/// </summary>
/// <remarks>
/// Do not also call the core <c>AddTarsLogging</c>/<c>AddTarsLoggingOtlpExporter</c>: pick one logging
/// path, or logs are exported twice. This project has no dependency on the OTel core — it is an
/// orthogonal, opt-in choice of logging framework.
/// </remarks>
public static class ObservabilitySerilogDI
{
    /// <summary>
    /// Registers Serilog as the logging provider with <c>Enrich.FromLogContext()</c> already enabled,
    /// then applies <paramref name="configure"/> to add sinks (e.g. <c>WriteTo.Console()</c> and
    /// <see cref="WriteToTarsOtlp"/>). Call once.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Callback that adds sinks to the Serilog configuration.</param>
    public static IServiceCollection AddTarsSerilog(
        this IServiceCollection services,
        Action<LoggerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSerilog((_, loggerConfiguration) =>
        {
            loggerConfiguration.Enrich.FromLogContext();
            configure(loggerConfiguration);
        });

        return services;
    }

    /// <summary>
    /// Adds the OpenTelemetry (OTLP) log sink, stamping the <c>service.name</c> resource attribute so
    /// logs line up with traces and metrics in the backend. The sink attaches the active trace and
    /// span ids automatically, giving log↔trace correlation. Use inside the
    /// <see cref="AddTarsSerilog"/> callback.
    /// </summary>
    /// <param name="loggerConfiguration">The Serilog configuration to add the sink to.</param>
    /// <param name="serviceName">The <c>service.name</c> resource attribute stamped on exported logs.</param>
    /// <param name="otlpEndpoint">
    /// OTLP endpoint (e.g. <c>http://localhost:4317</c>). When null, the sink falls back to the
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable and its default.
    /// </param>
    public static LoggerConfiguration WriteToTarsOtlp(
        this LoggerConfiguration loggerConfiguration,
        string serviceName,
        string? otlpEndpoint = null)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);

        return loggerConfiguration.WriteTo.OpenTelemetry(sink =>
        {
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                sink.Endpoint = otlpEndpoint;

            sink.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = serviceName
            };
        });
    }
}
