using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Pottmayer.Tars.Observability.Abstractions;

namespace Pottmayer.Tars.Observability.AspNetCore.Middleware;

/// <summary>
/// Gives every request a correlation id: reads it from the inbound
/// <see cref="TarsCorrelation.HeaderName"/> header or derives one, echoes it on the response,
/// stamps it on the current <see cref="Activity"/> and opens an <see cref="ILogger"/> scope so every
/// log line emitted while handling the request carries it. The scope is provider-agnostic — it flows
/// to the native OpenTelemetry log pipeline (when scopes are included) and to Serilog (via
/// <c>Enrich.FromLogContext</c>) alike.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>
    /// Creates a new middleware instance.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="logger">Logger used to open the correlation-id scope.</param>
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Resolves or derives the correlation id, tags the current span, echoes it on the response, and
    /// invokes the rest of the pipeline inside a logger scope carrying the id.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(TarsCorrelation.HeaderName, out var header)
            && !string.IsNullOrWhiteSpace(header)
                ? header.ToString()
                : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("n");

        Activity.Current?.SetTag(TarsCorrelation.PropertyName, correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[TarsCorrelation.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            [TarsCorrelation.PropertyName] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
