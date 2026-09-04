using Pottmayer.Tars.Ai.Abstractions;
using Pottmayer.Tars.Ai.Chat.Abstractions.Models;

namespace Pottmayer.Tars.Ai.Chat.Abstractions;

/// <summary>
/// Transport for a chat-completion model that supports tool calling. Stateless: it holds no
/// conversation and no retry policy — the caller owns both. Implementations throw
/// <see cref="AiException"/> on failure, with <see cref="AiException.IsPermanent"/> telling the caller
/// whether retrying can ever help.
/// </summary>
public interface IAiChatCompletionClient
{
    /// <summary>
    /// Sends one chat request and returns the model's reply. When the request carries tools, the reply
    /// may be a set of <see cref="ChatCompletion.ToolCalls"/> instead of prose.
    /// </summary>
    Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
