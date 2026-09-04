using System.Net;
using FluentAssertions;
using Pottmayer.Tars.Ai.Chat.Gemini;

namespace Pottmayer.Tars.Ai.Chat.Tests.Unit.Gemini;

public class GeminiErrorClassifierTests
{
    [Theory]
    [InlineData(400, true)]
    [InlineData(403, true)]
    [InlineData(404, true)]
    [InlineData(429, false)]
    [InlineData(500, false)]
    [InlineData(503, false)]
    public void Classify_sets_permanence_and_status_from_the_http_code(int status, bool permanent)
    {
        var exception = GeminiErrorClassifier.Classify("m", (HttpStatusCode)status, string.Empty);

        exception.IsPermanent.Should().Be(permanent);
        exception.StatusCode.Should().Be(status);
        exception.Provider.Should().Be("gemini");
        exception.Model.Should().Be("m");
    }

    [Fact]
    public void Classify_surfaces_the_api_error_message()
    {
        var body = """{"error":{"message":"API key not valid"}}""";

        var exception = GeminiErrorClassifier.Classify("m", HttpStatusCode.BadRequest, body);

        exception.Message.Should().Contain("API key not valid");
    }
}
