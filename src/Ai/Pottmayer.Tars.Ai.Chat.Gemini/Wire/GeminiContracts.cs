using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pottmayer.Tars.Ai.Chat.Gemini.Wire;

// The subset of the Generative Language v1beta generateContent contract this provider uses.
// Null members are dropped on the wire (see the serializer options in GeminiAiChatCompletionClient).

internal sealed record GeminiRequest(
    [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
    [property: JsonPropertyName("tools")] IReadOnlyList<GeminiTool>? Tools,
    [property: JsonPropertyName("systemInstruction")] GeminiContent? SystemInstruction,
    [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig? GenerationConfig);

internal sealed record GeminiContent(
    [property: JsonPropertyName("role")] string? Role,
    [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

internal sealed record GeminiPart(
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("functionCall")] GeminiFunctionCall? FunctionCall = null,
    [property: JsonPropertyName("functionResponse")] GeminiFunctionResponse? FunctionResponse = null);

internal sealed record GeminiFunctionCall(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("args")] JsonElement? Args);

internal sealed record GeminiFunctionResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("response")] JsonElement Response);

internal sealed record GeminiTool(
    [property: JsonPropertyName("functionDeclarations")] IReadOnlyList<GeminiFunctionDeclaration> FunctionDeclarations);

internal sealed record GeminiFunctionDeclaration(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("parameters")] JsonElement? Parameters);

internal sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("temperature")] double? Temperature);

internal sealed record GeminiResponse(
    [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates,
    [property: JsonPropertyName("usageMetadata")] GeminiUsage? UsageMetadata);

internal sealed record GeminiCandidate(
    [property: JsonPropertyName("content")] GeminiContent? Content,
    [property: JsonPropertyName("finishReason")] string? FinishReason);

internal sealed record GeminiUsage(
    [property: JsonPropertyName("promptTokenCount")] int PromptTokenCount,
    [property: JsonPropertyName("candidatesTokenCount")] int CandidatesTokenCount);
