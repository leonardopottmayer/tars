namespace Pottmayer.Tars.Ai.Chat.Abstractions.Models;

/// <summary>Who authored a chat message. Maps to the provider's own role names on the wire.</summary>
public enum ChatRole
{
    System,
    User,
    Assistant,
    Tool,
}
