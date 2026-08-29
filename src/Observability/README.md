# Pottmayer.Tars.Observability

Observability building blocks: OpenTelemetry traces, metrics and logs over OTLP, .NET runtime
metrics, and trace correlation. The **core is logging-provider-agnostic** — it logs through the
native `ILogger` pipeline; Serilog is an **opt-in** provider in a separate project, the same way
`Caching.Redis` or `Communication.Email.MailKit` are. There is **no single "add everything" method** —
you compose the pieces you want, in the order below.

## Projects

| Project | Contains |
| --- | --- |
| `Pottmayer.Tars.Observability.Abstractions` | Shared vocabulary (`TarsTelemetry`, `TarsCorrelation`). No dependencies — any building block can emit signals against it. |
| `Pottmayer.Tars.Observability` | Options + granular DI for the OTel pipeline and **native `ILogger`** logging. Host-agnostic, no third-party logging framework. |
| `Pottmayer.Tars.Observability.Serilog` | Opt-in Serilog logging provider (console + OTLP sink). Alternative to the core's native logging. |
| `Pottmayer.Tars.Observability.AspNetCore` | Request/HTTP-client instrumentation and the (provider-agnostic) correlation-id middleware. |

## Which method does what

### Options — `ObservabilityOptionsDI`
- `AddTarsObservabilityOptions()` — binds `Tars:Observability` (Enabled / ServiceName / ServiceVersion / OtlpEndpoint), defaulting `ServiceName` to the app name.

### OTel pipeline — `ObservabilityServicesDI`
- `AddTarsObservabilityResource(serviceName, serviceVersion?)` — sets `service.name` / `service.version` on every signal.
- `AddTarsTracing()` — adds the tracer, subscribed to every tars `ActivitySource`.
- `AddTarsTracingOtlpExporter(otlpEndpoint?)` — exports traces over OTLP. **Needs `AddTarsTracing` first.**
- `AddTarsMetrics()` — adds the meter provider, subscribed to every tars `Meter`.
- `AddTarsRuntimeMetrics()` — adds GC/heap/threadpool/exception metrics. **Needs `AddTarsMetrics` first.**
- `AddTarsMetricsOtlpExporter(otlpEndpoint?)` — exports metrics over OTLP. **Needs `AddTarsMetrics` first.**

### Logging — pick **one** path

**Native (core, `ObservabilityLoggingDI`)** — default, no extra dependencies:
- `AddTarsLogging()` — adds the native OpenTelemetry `ILogger` provider (scopes + formatted messages).
- `AddTarsLoggingOtlpExporter(otlpEndpoint?)` — exports logs over OTLP. **Needs `AddTarsLogging` first.**

**Serilog (opt-in, `Pottmayer.Tars.Observability.Serilog`)** — use *instead of* the native path:
- `AddTarsSerilog(configure)` — registers Serilog as the sole provider with `Enrich.FromLogContext()`; add sinks inside the callback.
- `WriteToTarsOtlp(serviceName, otlpEndpoint?)` — the OTLP log sink (used inside the `AddTarsSerilog` callback); attaches trace/span ids for log↔trace correlation.

> Do not combine the two — the native OTLP log exporter and the Serilog OTLP sink would each export the logs, duplicating them.

### ASP.NET Core — `ObservabilityAspNetCoreServicesDI`
- `AddTarsAspNetCoreTracing()` / `AddTarsHttpClientTracing()` — request / dependency spans. **Need `AddTarsTracing` first.**
- `AddTarsAspNetCoreMetrics()` / `AddTarsHttpClientMetrics()` — request / dependency metrics. **Need `AddTarsMetrics` first.**
- `UseTarsCorrelationId()` — correlation-id middleware; place it early in the pipeline.

## Canonical order (ASP.NET Core host)

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

// 5. Logging — choose ONE:

// 5a. Native ILogger + OTel (default, no extra deps)
builder.Services.AddTarsLogging();
builder.Services.AddTarsLoggingOtlpExporter(o.OtlpEndpoint);

// 5b. ...or Serilog (opt-in; requires Pottmayer.Tars.Observability.Serilog)
// builder.Services.AddTarsSerilog(logger => logger
//     .WriteTo.Console()
//     .WriteToTarsOtlp(serviceName, o.OtlpEndpoint));

var app = builder.Build();

// 6. Middleware (early)
app.UseTarsCorrelationId();
```

Within a signal the order is **pipeline → instrumentation → exporter**. Across signals the order is
free. Skip any block you don't want (e.g. omit the metrics section for traces-only).
