using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Pottmayer.Tars.Observability.Abstractions;
using Pottmayer.Tars.Observability.AspNetCore.DI;

namespace Pottmayer.Tars.Observability.Tests.Unit;

public class HttpClientTracingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Span_url_is_the_redacted_request_url_when_the_request_carries_one(bool redacted)
    {
        // The HttpClient span is only created by the real socket handler, so this calls a real local server.
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        app.MapGet("/bot123:SECRET/getMe", () => "ok");
        await app.StartAsync(TestContext.Current.CancellationToken);
        var baseUrl = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

        var spans = new List<Activity>();
        var services = new ServiceCollection();
        services.AddOpenTelemetry().WithTracing(t => t.AddProcessor(new CaptureProcessor(spans)));
        services.AddTarsHttpClientTracing();
        await using var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<TracerProvider>();

        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/bot123:SECRET/getMe");
        if (redacted)
            request.Options.Set(TarsTelemetry.RedactedRequestUrl, $"{baseUrl}/bot***/getMe");
        (await http.SendAsync(request, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var url = spans.Should().ContainSingle().Which.GetTagItem("url.full");
        url.Should().Be(redacted ? $"{baseUrl}/bot***/getMe" : $"{baseUrl}/bot123:SECRET/getMe");
    }

    private sealed class CaptureProcessor(List<Activity> spans) : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity data) => spans.Add(data);
    }
}
