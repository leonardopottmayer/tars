namespace Pottmayer.Tars.Ai.Chat.Abstractions.Models;

/// <summary>
/// One turn in a chat. An assistant turn may carry <see cref="ToolCalls"/> instead of (or alongside)
/// <see cref="Content"/> when the model chose to call tools. A user turn may carry
/// <see cref="Attachments"/> (audio, images) alongside its text.
/// </summary>
public sealed record ChatMessage(
    ChatRole Role,
    string? Content,
    IReadOnlyList<ToolCall>? ToolCalls = null,
    IReadOnlyList<ChatAttachment>? Attachments = null)
{
    public static ChatMessage System(string content) => new(ChatRole.System, content);

    public static ChatMessage User(string content) => new(ChatRole.User, content);

    public static ChatMessage User(string content, params IReadOnlyList<ChatAttachment> attachments)
        => new(ChatRole.User, content, Attachments: attachments);
}
