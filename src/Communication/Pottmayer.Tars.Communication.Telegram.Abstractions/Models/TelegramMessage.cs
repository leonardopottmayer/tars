namespace Pottmayer.Tars.Communication.Telegram.Abstractions.Models;

/// <summary>
/// A single outbound message. <see cref="ChatId"/> is the numeric chat id as a string (Telegram also
/// accepts <c>@channelusername</c>). <see cref="Text"/> must already be escaped for
/// <see cref="ParseMode"/> — see <see cref="TelegramText"/>.
/// </summary>
public sealed record TelegramMessage(
    string ChatId,
    string Text,
    TelegramParseMode ParseMode = TelegramParseMode.None,
    InlineKeyboard? Keyboard = null,
    bool DisableNotification = false,
    bool DisableLinkPreview = false,
    long? ReplyToMessageId = null);
