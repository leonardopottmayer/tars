using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Ai.Abstractions;
using Pottmayer.Tars.Ai.Chat.Abstractions;
using Pottmayer.Tars.Ai.Chat.Abstractions.Models;
using Pottmayer.Tars.Ai.Chat.DI;
using Pottmayer.Tars.Ai.Chat.Gemini;
using Pottmayer.Tars.Ai.Chat.Gemini.DI;
using Pottmayer.Tars.Ai.Chat.Gemini.Options;

namespace Pottmayer.Tars.Ai.Chat.Tests.Unit.Gemini;

public class GeminiAiChatCompletionClientTests
{
    [Fact]
    public async Task CompleteAsync_posts_to_the_model_endpoint_and_returns_the_text()
    {
        const string json = """
            {"candidates":[{"content":{"role":"model","parts":[{"text":"hi there"}]}}],
             "usageMetadata":{"promptTokenCount":5,"candidatesTokenCount":2}}
            """;
        var handler = new StubHandler(_ => Ok(json));
        var client = Client(handler, defaultKey: "k");

        var result = await client.CompleteAsync(new ChatRequest("gemini-2.0-flash", [ChatMessage.User("hi")]));

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.AbsolutePath.Should().Be("/v1beta/models/gemini-2.0-flash:generateContent");
        handler.LastBody.Should().Contain("\"contents\"");
        result.Message.Content.Should().Be("hi there");
        result.Usage.Should().Be(new TokenUsage(5, 2));
    }

    [Fact]
    public async Task CompleteAsync_uses_the_options_key_when_the_request_has_none()
    {
        var handler = new StubHandler(_ => Ok(EmptyReply));
        var client = Client(handler, defaultKey: "options-key");

        await client.CompleteAsync(new ChatRequest("m", [ChatMessage.User("hi")]));

        SentKey(handler).Should().Be("options-key");
    }

    [Fact]
    public async Task CompleteAsync_lets_the_request_key_override_the_options_key()
    {
        var handler = new StubHandler(_ => Ok(EmptyReply));
        var client = Client(handler, defaultKey: "options-key");

        await client.CompleteAsync(new ChatRequest("m", [ChatMessage.User("hi")], ApiKey: "user-key"));

        SentKey(handler).Should().Be("user-key");
    }

    [Fact]
    public async Task CompleteAsync_throws_a_permanent_error_when_no_key_is_available()
    {
        var handler = new StubHandler(_ => Ok(EmptyReply));
        var client = Client(handler, defaultKey: null);

        var act = () => client.CompleteAsync(new ChatRequest("m", [ChatMessage.User("hi")]));

        (await act.Should().ThrowAsync<AiException>()).Which.IsPermanent.Should().BeTrue();
        handler.LastRequest.Should().BeNull("the request must not be sent without a key");
    }

    [Fact]
    public async Task CompleteAsync_throws_a_permanent_error_on_a_client_status()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"bad"}}"""),
        });
        var client = Client(handler, defaultKey: "k");

        var act = () => client.CompleteAsync(new ChatRequest("m", [ChatMessage.User("x")]));

        (await act.Should().ThrowAsync<AiException>()).Which.IsPermanent.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_throws_a_transient_error_on_a_server_status()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent(string.Empty),
        });
        var client = Client(handler, defaultKey: "k");

        var act = () => client.CompleteAsync(new ChatRequest("m", [ChatMessage.User("x")]));

        (await act.Should().ThrowAsync<AiException>()).Which.IsPermanent.Should().BeFalse();
    }

    [Fact]
    public void AddTarsAiChatGemini_registers_the_client_under_the_gemini_key_and_via_the_factory()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddTarsAiChatGeminiOptions(configure: o => { });
        builder.Services.AddTarsAiClientFactory();
        builder.Services.AddTarsAiChatGeminiHttpClient();
        builder.Services.AddTarsAiChatCompletionClientGemini();

        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredKeyedService<IAiChatCompletionClient>("gemini").Should().BeOfType<GeminiAiChatCompletionClient>();
        provider.GetRequiredService<IAiChatCompletionClientFactory>().GetClient("gemini").Should().BeOfType<GeminiAiChatCompletionClient>();
    }

    private const string EmptyReply =
        """{"candidates":[{"content":{"role":"model","parts":[{"text":"ok"}]}}]}""";

    private static GeminiAiChatCompletionClient Client(HttpMessageHandler handler, string? defaultKey)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://gen.example/") };
        var options = Microsoft.Extensions.Options.Options.Create(new GeminiAiOptions { ApiKey = defaultKey ?? string.Empty });
        return new GeminiAiChatCompletionClient(http, options);
    }

    private static string? SentKey(StubHandler handler)
        => handler.LastRequest!.Headers.TryGetValues("x-goog-api-key", out var values) ? values.Single() : null;

    private static HttpResponseMessage Ok(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return responder(request);
        }
    }
}
