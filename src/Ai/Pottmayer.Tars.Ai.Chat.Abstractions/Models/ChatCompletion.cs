namespace Pottmayer.Tars.Ai.Chat.Abstractions.Models;

/// <summary>The model's reply to a <see cref="ChatRequest"/>.</summary>
public sealed record ChatCompletion(
    string Model,
    ChatMessage Message,
    TokenUsage Usage)
{
    /// <summary>The tool calls the model chose, or an empty list when it replied in prose.</summary>
    public IReadOnlyList<ToolCall> ToolCalls => Message.ToolCalls ?? [];
}
