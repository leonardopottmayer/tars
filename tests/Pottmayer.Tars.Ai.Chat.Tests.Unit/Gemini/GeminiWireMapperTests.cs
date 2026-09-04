using System.Text.Json;
using FluentAssertions;
using Pottmayer.Tars.Ai.Chat.Abstractions.Models;
using Pottmayer.Tars.Ai.Chat.Gemini.Wire;

namespace Pottmayer.Tars.Ai.Chat.Tests.Unit.Gemini;

public class GeminiWireMapperTests
{
    [Fact]
    public void ToWireRequest_maps_system_user_tools_and_temperature()
    {
        var schema = """{"type":"object","properties":{"q":{"type":"string"}}}""";
        var request = new ChatRequest(
            "gemini-2.0-flash",
            [ChatMessage.System("be terse"), ChatMessage.User("hi")],
            [new ToolDefinition("search", "search things", schema)],
            Temperature: 0);

        var wire = GeminiWireMapper.ToWireRequest(request);

        wire.SystemInstruction!.Parts.Single().Text.Should().Be("be terse");
        wire.Contents.Should().ContainSingle();
        wire.Contents[0].Role.Should().Be("user");
        wire.Contents[0].Parts.Single().Text.Should().Be("hi");
        wire.Tools!.Single().FunctionDeclarations.Single().Name.Should().Be("search");
        wire.Tools![0].FunctionDeclarations[0].Parameters!.Value.GetProperty("type").GetString().Should().Be("object");
        wire.GenerationConfig!.Temperature.Should().Be(0);
    }

    [Fact]
    public void ToWireRequest_pairs_a_tool_result_with_the_call_it_answers()
    {
        var args = JsonDocument.Parse("""{"city":"SP"}""").RootElement;
        var request = new ChatRequest(
            "m",
            [
                ChatMessage.User("weather?"),
                new ChatMessage(ChatRole.Assistant, null, [new ToolCall("get_weather", args)]),
                new ChatMessage(ChatRole.Tool, """{"temp":25}"""),
            ]);

        var wire = GeminiWireMapper.ToWireRequest(request);

        wire.Contents.Should().HaveCount(3);
        wire.Contents[1].Role.Should().Be("model");
        wire.Contents[1].Parts.Single().FunctionCall!.Name.Should().Be("get_weather");

        var response = wire.Contents[2].Parts.Single().FunctionResponse!;
        response.Name.Should().Be("get_weather");
        response.Response.GetProperty("result").GetProperty("temp").GetInt32().Should().Be(25);
    }

    [Fact]
    public void ToCompletion_reads_text_and_usage()
    {
        var response = new GeminiResponse(
            [new GeminiCandidate(new GeminiContent("model", [new GeminiPart(Text: "hello")]), "STOP")],
            new GeminiUsage(10, 3));

        var completion = GeminiWireMapper.ToCompletion(new ChatRequest("m", []), response);

        completion.Model.Should().Be("m");
        completion.Message.Content.Should().Be("hello");
        completion.ToolCalls.Should().BeEmpty();
        completion.Usage.Should().Be(new TokenUsage(10, 3));
    }

    [Fact]
    public void ToCompletion_reads_a_function_call()
    {
        var args = JsonDocument.Parse("""{"a":1}""").RootElement;
        var response = new GeminiResponse(
            [new GeminiCandidate(new GeminiContent("model", [new GeminiPart(FunctionCall: new GeminiFunctionCall("do", args))]), "STOP")],
            null);

        var completion = GeminiWireMapper.ToCompletion(new ChatRequest("m", []), response);

        completion.Message.Content.Should().BeNull();
        completion.ToolCalls.Should().ContainSingle();
        completion.ToolCalls[0].Name.Should().Be("do");
        completion.ToolCalls[0].Arguments.GetProperty("a").GetInt32().Should().Be(1);
        completion.Usage.Should().Be(new TokenUsage(0, 0));
    }
}
