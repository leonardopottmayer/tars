# Observability Configuration

## Options — `Tars:Observability`

```json
// appsettings.json
{
  "Tars": {
    "Observability": {
      "Enabled": true,
      "ServiceName": "pandora",
      "ServiceVersion": "1.0.0",
      "OtlpEndpoint": "http://localhost:4317"
    }
  }
}
```

| Property | Default | Role |
|---|---|---|
| `Enabled` | `true` | Master switch consumed by the host to skip the wiring entirely. |
| `ServiceName` | app name | Resource attribute `service.name`. Falls back to the host's application name when left empty. |
| `ServiceVersion` | `null` | Resource attribute `service.version`. |
| `OtlpEndpoint` | `null` | OTLP endpoint (gRPC). When null, the SDK and the Serilog sink fall back to `OTEL_EXPORTER_OTLP_ENDPOINT` and their localhost defaults. |

Bind them with `AddTarsObservabilityOptions()` (default section `Tars:Observability`, overridable via
`sectionName`, with a `configure` callback applied over the bound values). `ServiceName` is validated
to be non-empty when `Enabled` is true.

---

## The methods

There is no aggregate method — you compose per concern. Within a signal the order is
**pipeline → instrumentation → exporter**; across signals the order is free.

### OTel pipeline — `Pottmayer.Tars.Observability`

| Method | Does |
|---|---|
| `AddTarsObservabilityResource(serviceName, serviceVersion?)` | Sets `service.name` / `service.version` on every signal. |
| `AddTarsTracing()` | Adds the tracer, subscribed to every tars `ActivitySource`. |
| `AddTarsTracingOtlpExporter(otlpEndpoint?)` | Exports traces over OTLP. Needs `AddTarsTracing`. |
| `AddTarsMetrics()` | Adds the meter provider, subscribed to every tars `Meter`. |
| `AddTarsRuntimeMetrics()` | Adds GC/heap/thread-pool/exception metrics. Needs `AddTarsMetrics`. |
| `AddTarsMetricsOtlpExporter(otlpEndpoint?)` | Exports metrics over OTLP. Needs `AddTarsMetrics`. |

### Logging — pick one path

| Method | Package | Does |
|---|---|---|
| `AddTarsLogging()` | Runtime (core) | Native OpenTelemetry `ILogger` provider (scopes + formatted messages). |
| `AddTarsLoggingOtlpExporter(otlpEndpoint?)` | Runtime (core) | Exports logs over OTLP. Needs `AddTarsLogging`. |
| `AddTarsSerilog(configure)` | Serilog provider | Serilog as the sole provider with `Enrich.FromLogContext()`; add sinks in the callback. |
| `WriteToTarsOtlp(serviceName, otlpEndpoint?)` | Serilog provider | The OTLP log sink, used inside the `AddTarsSerilog` callback. |

See [logging.md](./logging.md) for choosing between the two.

### ASP.NET Core — `Pottmayer.Tars.Observability.AspNetCore`

| Method | Does |
|---|---|
| `AddTarsAspNetCoreTracing()` / `AddTarsHttpClientTracing()` | Request / dependency spans. Need `AddTarsTracing`. |
| `AddTarsAspNetCoreMetrics()` / `AddTarsHttpClientMetrics()` | Request / dependency metrics. Need `AddTarsMetrics`. |
| `UseTarsCorrelationId()` | Correlation-id middleware; place it early in the pipeline. |

---

## Canonical order (ASP.NET Core host, native logging)

```csharp
// 1. Options
builder.AddTarsObservabilityOptions();
var o = builder.Configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
        ?? new ObservabilityOptions();
var serviceName = string.IsNullOrWhiteSpace(o.ServiceName) ? builder.Environment.ApplicationName : o.ServiceName;

// 2. Resource (once, before the pipelines)
builder.Services.AddTarsObservabilityResource(serviceName, o.ServiceVersion);

// 3. Tracing: pipeline -> instrumentation -> exporter
builder.Services.AddTarsTracing();
builder.Services.AddTarsAspNetCoreTracing();
builder.Services.AddTarsHttpClientTracing();
builder.Services.AddTarsTracingOtlpExporter(o.OtlpEndpoint);

// 4. Metrics: pipeline -> instrumentation -> runtime -> exporter
builder.Services.AddTarsMetrics();
builder.Services.AddTarsAspNetCoreMetrics();
builder.Services.AddTarsHttpClientMetrics();
builder.Services.AddTarsRuntimeMetrics();
builder.Services.AddTarsMetricsOtlpExporter(o.OtlpEndpoint);

// 5. Logging (native)
builder.Services.AddTarsLogging();
builder.Services.AddTarsLoggingOtlpExporter(o.OtlpEndpoint);

var app = builder.Build();

// 6. Middleware (early)
app.UseTarsCorrelationId();
```

Skip any block you don't want (e.g. omit the metrics section for traces-only). To use Serilog instead
of the native logging, replace step 5 — see [logging.md](./logging.md).
