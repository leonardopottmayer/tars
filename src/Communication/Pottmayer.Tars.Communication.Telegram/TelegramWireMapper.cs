using Pottmayer.Tars.Communication.Telegram.Abstractions.Models;
using Pottmayer.Tars.Communication.Telegram.Wire;

namespace Pottmayer.Tars.Communication.Telegram;

/// <summary>Translates between the Bot API wire shapes and the public models.</summary>
internal static class TelegramWireMapper
{
    internal static TelegramUpdate ToUpdate(WireUpdate wire) => new(
        wire.UpdateId,
        wire.Message is null ? null : ToIncomingMessage(wire.Message),
        wire.CallbackQuery is null ? null : ToCallbackQuery(wire.CallbackQuery));

    internal static TelegramIncomingMessage ToIncomingMessage(WireMessage wire) => new(
        wire.MessageId,
        new TelegramChat(wire.Chat.Id, wire.Chat.Type),
        wire.From is null ? null : ToSender(wire.From),
        DateTimeOffset.FromUnixTimeSeconds(wire.Date),
        // A caption is the human text of a media message, so callers see it in the same place.
        wire.Text ?? wire.Caption,
        ToMedia(wire),
        wire.ReplyToMessage?.MessageId);

    internal static TelegramCallbackQuery ToCallbackQuery(WireCallbackQuery wire) => new(
        wire.Id,
        ToSender(wire.From),
        wire.Data,
        wire.Message is null ? null : new TelegramChat(wire.Message.Chat.Id, wire.Message.Chat.Type),
        wire.Message?.MessageId);

    internal static TelegramSender ToSender(WireUser wire) => new(
        wire.Id, wire.Username, wire.FirstName, wire.LastName, wire.LanguageCode, wire.IsBot);

    /// <summary>
    /// Picks the one attachment a message carries. Telegram sets exactly one of these fields, so the
    /// order only decides what wins in a shape we have not seen.
    /// </summary>
    internal static TelegramMedia? ToMedia(WireMessage wire)
    {
        if (wire.Voice is not null)
            return ToMedia(wire.Voice, TelegramMediaKind.Voice);

        if (wire.Audio is not null)
            return ToMedia(wire.Audio, TelegramMediaKind.Audio);

        if (wire.VideoNote is not null)
            return ToMedia(wire.VideoNote, TelegramMediaKind.VideoNote);

        if (wire.Video is not null)
            return ToMedia(wire.Video, TelegramMediaKind.Video);

        if (wire.Document is not null)
            return ToMedia(wire.Document, TelegramMediaKind.Document);

        // Photos arrive as every available size, smallest first; the last one is the original.
        if (wire.Photo is { Length: > 0 } photo)
            return ToMedia(photo[^1], TelegramMediaKind.Photo);

        return null;
    }

    private static TelegramMedia ToMedia(WireFile file, TelegramMediaKind kind) => new(
        kind, file.FileId, file.MimeType, file.Duration, file.FileSize, file.FileName);

    internal static SendMessageRequest ToSendRequest(TelegramMessage message) => new()
    {
        ChatId = message.ChatId,
        Text = message.Text,
        ParseMode = ToParseMode(message.ParseMode),
        DisableNotification = message.DisableNotification ? true : null,
        LinkPreviewOptions = message.DisableLinkPreview ? new WireLinkPreviewOptions { IsDisabled = true } : null,
        ReplyParameters = message.ReplyToMessageId is { } id ? new WireReplyParameters { MessageId = id } : null,
        ReplyMarkup = message.Keyboard is null ? null : ToKeyboardMarkup(message.Keyboard),
    };

    internal static string? ToParseMode(TelegramParseMode mode) => mode switch
    {
        TelegramParseMode.MarkdownV2 => "MarkdownV2",
        TelegramParseMode.Html => "HTML",
        _ => null,
    };

    internal static WireInlineKeyboardMarkup ToKeyboardMarkup(InlineKeyboard keyboard) => new()
    {
        InlineKeyboard = [.. keyboard.Rows.Select(row => row.Select(ToKeyboardButton).ToList())],
    };

    private static WireInlineKeyboardButton ToKeyboardButton(InlineButton button) => new()
    {
        Text = button.Label,
        CallbackData = button.CallbackData,
        Url = button.Url?.ToString(),
    };
}
