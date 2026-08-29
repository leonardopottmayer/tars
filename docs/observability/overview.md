# Observability — Overview

## Packages

| Package | Level | Role |
|---|---|---|
| `Pottmayer.Tars.Observability.Abstractions` | Abstractions | Shared vocabulary: `TarsTelemetry` (source/meter naming), `TarsCorrelation` (header and property names). No dependencies. |
| `Pottmayer.Tars.Observability` | Runtime | Options + granular DI for the OpenTelemetry pipeline (traces, metrics, logs) and native `ILogger` logging. Host-agnostic. |
| `Pottmayer.Tars.Observability.Serilog` | Provider | Opt-in Serilog logging provider (console + OTLP sink). Alternative to the core's native logging. |
| `Pottmayer.Tars.Observability.AspNetCore` | Host Integration | ASP.NET Core / HttpClient instrumentation and the correlation-id middleware. |

---

## Core principle

The core is **logging-provider-agnostic**. It emits traces, metrics and logs through OpenTelemetry
and the native `ILogger` pipeline, and exports everything over **OTLP** — so any backend that speaks
OTLP (an OpenTelemetry Collector, the .NET Aspire Dashboard, Grafana/Tempo/Loki/Prometheus, a SaaS)
works without a code change. Serilog is not baked in: it is a separate, opt-in provider, the same way
`Caching.Redis` or `Communication.Email.MailKit` are.

There is **no single "add everything" method**. Each concern (resource, tracing, metrics, logging,
each exporter, each instrumentation) has its own `AddTars*` method, so a host composes exactly the
signals it wants. See [configuration.md](./configuration.md) for the canonical order.

---

## What the family provides

- **Traces** subscribed to every tars `ActivitySource` via the `Pottmayer.Tars.*` wildcard, plus
  ASP.NET Core (inbound requests) and HttpClient (outbound calls) instrumentation.
- **Metrics** subscribed to every tars `Meter`, plus .NET runtime metrics and the same ASP.NET
  Core / HttpClient instrumentation.
- **Logs** via the native `ILogger` pipeline (default) or Serilog (opt-in) — one or the other.
- **OTLP export** for all three signals, sharing one resource (`service.name` / `service.version`).
- **Correlation id**: a middleware that gives every request an id, echoes it on the response, tags
  the active span and opens a provider-agnostic `ILogger` scope so every log line carries it.

---

## Shared vocabulary

### `TarsTelemetry`

Naming conventions so a single wildcard subscription captures every family's signals. Building blocks
create their `ActivitySource` / `Meter` with `TarsTelemetry.Name("Messaging")`
(→ `Pottmayer.Tars.Messaging`); the pipeline subscribes with `TarsTelemetry.Wildcard`
(`Pottmayer.Tars.*`).

```csharp
public static class TarsTelemetry
{
    public const string RootName = "Pottmayer.Tars";
    public const string Wildcard = "Pottmayer.Tars.*";
    public static string Name(string family);
}
```

### `TarsCorrelation`

The header (`X-Correlation-ID`) and the log-property / span-tag key (`CorrelationId`) used by the
correlation-id middleware.

---

## Minimal registration

```csharp
// Program.cs (ASP.NET Core host)
builder.AddTarsObservabilityOptions();

builder.Services.AddTarsObservabilityResource("pandora");
builder.Services.AddTarsTracing();
builder.Services.AddTarsAspNetCoreTracing();
builder.Services.AddTarsTracingOtlpExporter("http://localhost:4317");
builder.Services.AddTarsLogging();
builder.Services.AddTarsLoggingOtlpExporter("http://localhost:4317");

var app = builder.Build();
app.UseTarsCorrelationId();
```

For the full sequence, all methods and appsettings, see [configuration.md](./configuration.md).

---

## Topics

- [Configuration](./configuration.md): `Tars:Observability` options, the canonical composition order, every `AddTars*` method
- [Logging](./logging.md): the native `ILogger` path vs the Serilog provider, and how correlation flows through both
