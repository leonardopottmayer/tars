namespace Pottmayer.Tars.Communication.Telegram.Abstractions.Models;

/// <summary>How Telegram should interpret the message text.</summary>
public enum TelegramParseMode
{
    /// <summary>Plain text. Nothing is interpreted, so nothing needs escaping.</summary>
    None = 0,

    /// <summary>Telegram's MarkdownV2 dialect. Escape with <see cref="TelegramText.EscapeMarkdownV2"/>.</summary>
    MarkdownV2 = 1,

    /// <summary>The Bot API's HTML subset. Escape with <see cref="TelegramText.EscapeHtml"/>.</summary>
    Html = 2,
}
