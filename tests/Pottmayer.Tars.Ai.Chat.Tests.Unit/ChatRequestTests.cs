using FluentAssertions;
using Pottmayer.Tars.Ai.Chat.Abstractions.Models;

namespace Pottmayer.Tars.Ai.Chat.Tests.Unit;

public class ChatRequestTests
{
    [Fact]
    public void ToString_redacts_the_api_key()
    {
        var request = new ChatRequest("m", [ChatMessage.User("hi")], ApiKey: "super-secret");

        var text = request.ToString();

        text.Should().NotContain("super-secret");
        text.Should().Contain("ApiKey = ***");
    }
}
