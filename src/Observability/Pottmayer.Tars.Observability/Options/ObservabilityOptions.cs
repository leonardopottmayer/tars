namespace Pottmayer.Tars.Observability.Options;

/// <summary>Options for the tars observability foundation, bound from configuration.</summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Tars:Observability";

    /// <summary>Master switch. When <c>false</c>, <c>AddTarsObservability</c> is a no-op.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Logical service name stamped on every signal (resource attribute <c>service.name</c>).
    /// When empty, the wiring falls back to the host's application name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Optional service version (resource attribute <c>service.version</c>).</summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// OTLP endpoint (gRPC), e.g. <c>http://localhost:4317</c>. When null, the OpenTelemetry SDK and
    /// the Serilog OTLP sink fall back to the <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable
    /// and their own localhost defaults.
    /// </summary>
    public string? OtlpEndpoint { get; set; }
}
