using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Pottmayer.Tars.Observability.Abstractions;
using Pottmayer.Tars.Observability.AspNetCore.Middleware;

namespace Pottmayer.Tars.Observability.Tests.Unit;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Uses_the_inbound_header_when_present()
    {
        var (context, feature, logger) = BuildContext(inboundCorrelationId: "abc-123");
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);
        await feature.FireOnStartingAsync();

        feature.Headers[TarsCorrelation.HeaderName].ToString().Should().Be("abc-123");
        logger.LastScope.Should().ContainKey(TarsCorrelation.PropertyName)
            .WhoseValue.Should().Be("abc-123");
    }

    [Fact]
    public async Task Generates_an_id_and_echoes_it_when_no_header_is_present()
    {
        var (context, feature, logger) = BuildContext(inboundCorrelationId: null);
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);
        await feature.FireOnStartingAsync();

        var echoed = feature.Headers[TarsCorrelation.HeaderName].ToString();
        echoed.Should().NotBeNullOrWhiteSpace();
        logger.LastScope.Should().ContainKey(TarsCorrelation.PropertyName)
            .WhoseValue.Should().Be(echoed);
    }

    [Fact]
    public async Task Derives_the_id_from_the_active_trace_and_tags_the_activity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("test-source");
        using var activity = source.StartActivity("request");
        activity.Should().NotBeNull();

        var (context, feature, logger) = BuildContext(inboundCorrelationId: null);
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);
        await feature.FireOnStartingAsync();

        var expected = activity!.TraceId.ToString();
        feature.Headers[TarsCorrelation.HeaderName].ToString().Should().Be(expected);
        activity.GetTagItem(TarsCorrelation.PropertyName).Should().Be(expected);
    }

    private static (HttpContext Context, RecordingResponseFeature Feature, RecordingLogger Logger) BuildContext(
        string? inboundCorrelationId)
    {
        var requestFeature = new HttpRequestFeature { Headers = new HeaderDictionary() };
        if (inboundCorrelationId is not null)
            requestFeature.Headers[TarsCorrelation.HeaderName] = inboundCorrelationId;

        var responseFeature = new RecordingResponseFeature();

        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(requestFeature);
        features.Set<IHttpResponseFeature>(responseFeature);

        return (new DefaultHttpContext(features), responseFeature, new RecordingLogger());
    }
}

internal sealed class RecordingResponseFeature : IHttpResponseFeature
{
    private readonly List<(Func<object, Task> Callback, object State)> _onStarting = new();

    public int StatusCode { get; set; } = 200;
    public string? ReasonPhrase { get; set; }
    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
    public Stream Body { get; set; } = Stream.Null;
    public bool HasStarted { get; private set; }

    public void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));

    public void OnCompleted(Func<object, Task> callback, object state) { }

    public async Task FireOnStartingAsync()
    {
        HasStarted = true;
        foreach (var (callback, state) in _onStarting)
            await callback(state);
    }
}

internal sealed class RecordingLogger : ILogger<CorrelationIdMiddleware>
{
    public IReadOnlyDictionary<string, object?>? LastScope { get; private set; }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        if (state is IEnumerable<KeyValuePair<string, object>> pairs)
            LastScope = pairs.ToDictionary(pair => pair.Key, pair => (object?)pair.Value);

        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    { }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
