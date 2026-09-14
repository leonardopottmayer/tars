using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pottmayer.Tars.Ai.Abstractions;
using Pottmayer.Tars.Ai.Chat.Abstractions;
using Pottmayer.Tars.Ai.Chat.Abstractions.Models;
using Pottmayer.Tars.Ai.Chat.DI;
using Pottmayer.Tars.Ai.Chat.Gemini;
using Pottmayer.Tars.Ai.Chat.Gemini.DI;
using Pottmayer.Tars.Ai.Chat.Gemini.Options;

namespace Pottmayer.Tars.Ai.Chat.Tests.Unit.Gemini;

/// <summary>
/// Covers the retry handler added to the Gemini typed client by <c>AddTarsAiChatGeminiHttpClient</c>.
/// The client is resolved through the real DI pipeline so the handler is in effect; only the primary
/// HTTP handler is stubbed, counting how many times the request actually reached the wire.
/// </summary>
public class GeminiResilienceTests
{
    [Fact]
    public async Task A_transient_503_is_retried_and_then_succeeds()
    {
        var handler = new SequenceHandler(
            _ => ServiceUnavailable(),
            _ => ServiceUnavailable(),
            _ => Ok(Reply));

        var client = BuildClient(handler, maxRetryAttempts: 3);

        var result = await client.CompleteAsync(new ChatRequest("m", [ChatMessage.User("hi")]));

        result.Message.Content.Should().Be("ok");
        handler.Calls.Should().Be(3, "two 503s should be retried before the third attempt succeeds");
    }

    [Fact]
    public async Task A_transient_429_is_retried()
    {
        var handler = new SequenceHandler(
            _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent(string.Empty) },
            _ => Ok(Reply));

        var client = BuildClient(handler, maxRetryAttempts: 3);

        await client.CompleteAsync(new ChatRequest("m", [ChatMessage.User("hi")]));

        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task A_permanent_400_is_not_retried()
    {
        var handler = new SequenceHandler(
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"error":{"message":"bad"}}"""),
            });

        var client = BuildClient(handler, maxRetryAttempts: 3);

        var act = () => client.CompleteAsync(new ChatRequest("m", [ChatMessage.User("hi")]));

        (await act.Should().ThrowAsync<AiException>()).Which.IsPermanent.Should().BeTrue();
        handler.Calls.Should().Be(1, "a client error cannot be fixed by retrying");
    }

    [Fact]
    public async Task Retries_give_up_after_the_configured_attempts()
    {
        var handler = new SequenceHandler(_ => ServiceUnavailable()); // always transient

        var client = BuildClient(handler, maxRetryAttempts: 2);

        var act = () => client.CompleteAsync(new ChatRequest("m", [ChatMessage.User("hi")]));

        (await act.Should().ThrowAsync<AiException>()).Which.IsPermanent.Should().BeFalse();
        handler.Calls.Should().Be(3, "one initial attempt plus two retries");
    }

    [Fact]
    public async Task MaxRetryAttempts_zero_disables_retrying()
    {
        var handler = new SequenceHandler(_ => ServiceUnavailable());

        var client = BuildClient(handler, maxRetryAttempts: 0);

        var act = () => client.CompleteAsync(new ChatRequest("m", [ChatMessage.User("hi")]));

        await act.Should().ThrowAsync<AiException>();
        handler.Calls.Should().Be(1, "with retries disabled only the initial attempt is made");
    }

    private const string Reply =
        """{"candidates":[{"content":{"role":"model","parts":[{"text":"ok"}]}}]}""";

    private static IAiChatCompletionClient BuildClient(HttpMessageHandler primaryHandler, int maxRetryAttempts)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tars:Ai:Chat:Gemini:ApiKey"] = "k",
            ["Tars:Ai:Chat:Gemini:MaxRetryAttempts"] = maxRetryAttempts.ToString(),
            ["Tars:Ai:Chat:Gemini:RetryBaseDelay"] = "00:00:00", // keep the test fast; backoff shape is not asserted
        });
        builder.AddTarsAiChatGeminiOptions();
        builder.Services.AddTarsAiClientFactory();
        builder.Services.AddTarsAiChatGeminiHttpClient();
        builder.Services.AddTarsAiChatCompletionClientGemini();

        // Swap the primary handler for the stub while keeping the retry handler in the pipeline.
        builder.Services.AddHttpClient<GeminiAiChatCompletionClient>()
            .ConfigurePrimaryHttpMessageHandler(() => primaryHandler);

        var provider = builder.Services.BuildServiceProvider();
        return provider.GetRequiredService<IAiChatCompletionClientFactory>().GetClient("gemini");
    }

    private static HttpResponseMessage ServiceUnavailable()
        => new(HttpStatusCode.ServiceUnavailable) { Content = new StringContent(string.Empty) };

    private static HttpResponseMessage Ok(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>
    /// Returns each responder in turn, reusing the last one once the sequence is exhausted, and counts how
    /// many requests it saw. New response instances are created per call so retries never reuse a disposed one.
    /// </summary>
    private sealed class SequenceHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responders) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var index = Math.Min(Calls, responders.Length - 1);
            Calls++;
            return Task.FromResult(responders[index](request));
        }
    }
}
