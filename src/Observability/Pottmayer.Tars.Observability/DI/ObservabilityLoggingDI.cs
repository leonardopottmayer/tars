using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

namespace Pottmayer.Tars.Observability.DI;

/// <summary>
/// Provider-agnostic logging registrations built on the native <c>ILogger</c> pipeline exported over
/// OpenTelemetry. This is the default logging path and pulls in no third-party logging framework.
/// Scopes are included, so the correlation-id middleware (which uses <c>ILogger.BeginScope</c>) shows
/// up on every log record. This shares the resource configured by
/// <see cref="ObservabilityServicesDI.AddTarsObservabilityResource"/>.
/// </summary>
/// <remarks>
/// This is an alternative to the Serilog provider (<c>Pottmayer.Tars.Observability.Serilog</c>): pick
/// one logging path. Do not combine <see cref="AddTarsLoggingOtlpExporter"/> with the Serilog OTLP
/// sink, or logs will be exported twice.
/// </remarks>
public static class ObservabilityLoggingDI
{
    /// <summary>
    /// Adds the native OpenTelemetry logging provider to the <c>ILogger</c> pipeline, including scopes
    /// and formatted messages. Call before <see cref="AddTarsLoggingOtlpExporter"/>.
    /// </summary>
    public static IServiceCollection AddTarsLogging(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .WithLogging(
                _ => { },
                options =>
                {
                    options.IncludeScopes = true;
                    options.IncludeFormattedMessage = true;
                });
        return services;
    }

    /// <summary>
    /// Exports logs over OTLP. Requires <see cref="AddTarsLogging"/> to have been called.
    /// </summary>
    /// <param name="otlpEndpoint">
    /// OTLP endpoint (e.g. <c>http://localhost:4317</c>). When null, the SDK falls back to the
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable and its default.
    /// </param>
    public static IServiceCollection AddTarsLoggingOtlpExporter(
        this IServiceCollection services,
        string? otlpEndpoint = null)
    {
        services
            .AddOpenTelemetry()
            .WithLogging(logging => logging.AddOtlpExporter(exporter => ApplyEndpoint(exporter, otlpEndpoint)));
        return services;
    }

    private static void ApplyEndpoint(OtlpExporterOptions options, string? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint))
            options.Endpoint = new Uri(endpoint);
    }
}
