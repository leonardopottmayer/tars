namespace Pottmayer.Tars.Communication.Telegram.Abstractions;

/// <summary>
/// Resolves the <see cref="ITelegramClient"/> for a bot by name, so one application can run several bots
/// at once (e.g. <c>notifications</c> and <c>assistant</c>) — each with its own token — and pick one per
/// call. The names come from configuration (the keys under <c>Tars:Communication:Telegram:Bots</c>), so
/// adding a bot is a configuration entry, not a code change.
/// </summary>
public interface ITelegramClientFactory
{
    /// <summary>
    /// The reserved bot name resolved by the parameterless <see cref="GetClient()"/>. An application that
    /// runs a single bot configures it under this key and never has to name it at the call site.
    /// </summary>
    const string DefaultBotName = "default";

    /// <summary>
    /// Returns a client for the bot configured under <paramref name="name"/> (e.g. <c>assistant</c>), or the
    /// one under <see cref="DefaultBotName"/> when called without an argument. Throws
    /// <see cref="TelegramException"/> when no bot by that name is configured.
    /// </summary>
    ITelegramClient GetClient(string name = DefaultBotName);
}
