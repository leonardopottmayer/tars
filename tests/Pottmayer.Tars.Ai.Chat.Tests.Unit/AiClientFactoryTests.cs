using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Ai.Abstractions;
using Pottmayer.Tars.Ai.Chat.Abstractions;
using Pottmayer.Tars.Ai.Chat.Abstractions.Models;
using Pottmayer.Tars.Ai.Chat.DI;

namespace Pottmayer.Tars.Ai.Chat.Tests.Unit;

/// <summary>A stand-in provider so the factory can be tested without a real transport.</summary>
internal sealed class FakeChatClient(string tag) : IAiChatCompletionClient
{
    public string Tag => tag;

    public Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

public class AiClientFactoryTests
{
    [Fact]
    public void Factory_resolves_each_provider_by_its_own_name()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IAiChatCompletionClient>("openai", (_, _) => new FakeChatClient("openai"));
        services.AddKeyedTransient<IAiChatCompletionClient>("gemini", (_, _) => new FakeChatClient("gemini"));
        services.AddKeyedTransient<IAiChatCompletionClient>("anthropic", (_, _) => new FakeChatClient("anthropic"));
        services.AddTarsAiClientFactory();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IAiChatCompletionClientFactory>();

        factory.GetClient("openai").Should().BeOfType<FakeChatClient>().Which.Tag.Should().Be("openai");
        factory.GetClient("gemini").Should().BeOfType<FakeChatClient>().Which.Tag.Should().Be("gemini");
        factory.GetClient("anthropic").Should().BeOfType<FakeChatClient>().Which.Tag.Should().Be("anthropic");
    }

    [Fact]
    public void Factory_throws_a_permanent_error_for_an_unregistered_provider()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IAiChatCompletionClient>("openai", (_, _) => new FakeChatClient("openai"));
        services.AddTarsAiClientFactory();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IAiChatCompletionClientFactory>();

        var act = () => factory.GetClient("gemini");

        act.Should().Throw<AiException>().Which.IsPermanent.Should().BeTrue();
    }
}
