using Pottmayer.Tars.Ai.Abstractions;

namespace Pottmayer.Tars.Ai.Chat.Abstractions;

/// <summary>
/// Resolves the <see cref="IAiChatCompletionClient"/> for a named provider, so one application can host
/// several providers at once (e.g. <c>openai</c>, <c>gemini</c>) and pick per call —
/// typically from the user's profile. Each provider package registers its client under its own name, so
/// adding a provider is a registration, not a change here.
/// </summary>
public interface IAiChatCompletionClientFactory
{
    /// <summary>
    /// Returns the client registered for <paramref name="provider"/> (e.g. <c>openai</c>). Throws
    /// <see cref="AiException"/> when no provider by that name is registered.
    /// </summary>
    IAiChatCompletionClient GetClient(string provider);
}
