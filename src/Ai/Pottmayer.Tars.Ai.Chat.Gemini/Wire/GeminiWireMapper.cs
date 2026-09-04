using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Pottmayer.Tars.Ai.Chat.Abstractions.Models;

namespace Pottmayer.Tars.Ai.Chat.Gemini.Wire;

/// <summary>
/// Translates between the provider-agnostic chat model and Gemini's generateContent shape. Gemini has no
/// system role (system prompts go in <c>systemInstruction</c>) and pairs each tool result with the name
/// of the call it answers, which we recover from the assistant turns as we walk the conversation.
/// </summary>
internal static class GeminiWireMapper
{
    private static readonly JsonElement EmptyObject = ParseClone("{}");

    public static GeminiRequest ToWireRequest(ChatRequest request)
    {
        var contents = new List<GeminiContent>();
        var systemParts = new List<GeminiPart>();
        var pendingCallNames = new Queue<string>();

        foreach (var message in request.Messages)
        {
            switch (message.Role)
            {
                case ChatRole.System:
                    if (!string.IsNullOrEmpty(message.Content))
                        systemParts.Add(new GeminiPart(Text: message.Content));
                    break;

                case ChatRole.User:
                    contents.Add(new GeminiContent("user", [new GeminiPart(Text: message.Content ?? string.Empty)]));
                    break;

                case ChatRole.Assistant:
                    contents.Add(new GeminiContent("model", AssistantParts(message, pendingCallNames)));
                    break;

                case ChatRole.Tool:
                    var name = pendingCallNames.Count > 0
                        ? pendingCallNames.Dequeue()
                        : message.ToolCalls?.FirstOrDefault()?.Name ?? string.Empty;
                    contents.Add(new GeminiContent(
                        "user",
                        [new GeminiPart(FunctionResponse: new GeminiFunctionResponse(name, WrapToolResult(message.Content)))]));
                    break;
            }
        }

        var systemInstruction = systemParts.Count > 0 ? new GeminiContent(null, systemParts) : null;

        IReadOnlyList<GeminiTool>? tools = request.Tools is { Count: > 0 }
            ? [new GeminiTool(request.Tools.Select(ToFunctionDeclaration).ToList())]
            : null;

        var generationConfig = request.Temperature is not null
            ? new GeminiGenerationConfig(request.Temperature)
            : null;

        return new GeminiRequest(contents, tools, systemInstruction, generationConfig);
    }

    public static ChatCompletion ToCompletion(ChatRequest request, GeminiResponse response)
    {
        var parts = response.Candidates is { Count: > 0 }
            ? response.Candidates[0].Content?.Parts ?? []
            : [];

        var text = new StringBuilder();
        var toolCalls = new List<ToolCall>();

        foreach (var part in parts)
        {
            if (part.Text is not null)
                text.Append(part.Text);

            if (part.FunctionCall is not null)
                toolCalls.Add(new ToolCall(part.FunctionCall.Name, part.FunctionCall.Args ?? EmptyObject));
        }

        var usage = response.UsageMetadata is { } u
            ? new TokenUsage(u.PromptTokenCount, u.CandidatesTokenCount)
            : new TokenUsage(0, 0);

        var message = new ChatMessage(
            ChatRole.Assistant,
            text.Length > 0 ? text.ToString() : null,
            toolCalls.Count > 0 ? toolCalls : null);

        return new ChatCompletion(request.Model, message, usage);
    }

    private static IReadOnlyList<GeminiPart> AssistantParts(ChatMessage message, Queue<string> pendingCallNames)
    {
        var parts = new List<GeminiPart>();

        if (!string.IsNullOrEmpty(message.Content))
            parts.Add(new GeminiPart(Text: message.Content));

        if (message.ToolCalls is { Count: > 0 })
        {
            foreach (var call in message.ToolCalls)
            {
                parts.Add(new GeminiPart(FunctionCall: new GeminiFunctionCall(call.Name, call.Arguments)));
                pendingCallNames.Enqueue(call.Name);
            }
        }

        // Gemini rejects a content with no parts.
        if (parts.Count == 0)
            parts.Add(new GeminiPart(Text: string.Empty));

        return parts;
    }

    private static GeminiFunctionDeclaration ToFunctionDeclaration(ToolDefinition tool)
        => new(tool.Name, tool.Description, ParseClone(tool.ParametersJsonSchema));

    /// <summary>Gemini requires the tool result to be a JSON object; wrap the raw content under a key.</summary>
    private static JsonElement WrapToolResult(string? content)
    {
        JsonNode? value = null;
        if (content is not null)
        {
            try { value = JsonNode.Parse(content); }
            catch (JsonException) { value = JsonValue.Create(content); }
        }

        value ??= JsonValue.Create(content ?? string.Empty);
        var wrapped = new JsonObject { ["result"] = value };
        return JsonSerializer.SerializeToElement(wrapped);
    }

    private static JsonElement ParseClone(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
