# Observability — Logging

Logging has **two paths**. Pick exactly one — running both exports every log twice (the native OTLP
log exporter and the Serilog OTLP sink would each ship it).

## Path A — native `ILogger` (default)

No third-party logging framework. Logs go through the native `ILogger` pipeline and the OpenTelemetry
log provider, sharing the resource configured by `AddTarsObservabilityResource`.

```csharp
builder.Services.AddTarsLogging();                       // OTel ILogger provider, scopes included
builder.Services.AddTarsLoggingOtlpExporter(o.OtlpEndpoint);
```

Structured logging is done with `ILogger` message templates:

```csharp
logger.LogInformation("OFX import failed for {Account} on {File}", accountId, fileName);
```

This is the recommended default and the path the .NET Aspire Dashboard expects.

---

## Path B — Serilog (opt-in)

Reference `Pottmayer.Tars.Observability.Serilog` and register Serilog as the sole provider. Compose the
sinks in one call; `Enrich.FromLogContext()` is always enabled for you.

```csharp
builder.Services.AddTarsSerilog(logger => logger
    .WriteTo.Console()
    .WriteToTarsOtlp(serviceName, o.OtlpEndpoint));
```

Do **not** also call `AddTarsLogging` / `AddTarsLoggingOtlpExporter`.

> Namespace note: the project namespace `Pottmayer.Tars.Observability.Serilog` collides with the
> `Serilog` library namespace. Inside that project (and in code under an `...Observability.*`
> namespace that needs the library), reference it as `using global::Serilog;`.

---

## Correlation id across both paths

`UseTarsCorrelationId()` derives a correlation id (from the inbound `X-Correlation-ID` header, else the
active trace id, else a new guid), echoes it on the response, tags the current span, and opens an
`ILogger.BeginScope` with the `CorrelationId` property.

Because it uses the provider-agnostic `ILogger.BeginScope`, the id reaches both paths:

- **Native**: `AddTarsLogging` includes scopes, so the scope property is exported on every log record.
- **Serilog**: `Enrich.FromLogContext()` (enabled by `AddTarsSerilog`) turns the scope into a log
  property.

Either way, log records carry the same `CorrelationId` — and, because the middleware runs inside the
request's span, the trace and span ids line up too, giving log↔trace correlation in the backend.
