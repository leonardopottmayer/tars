namespace Pottmayer.Tars.Communication.Telegram.Wire;

// Wire shapes for the Bot API. They exist so the public models in Abstractions stay free of JSON
// concerns and of fields the caller has no use for. Property names map to snake_case through the
// serializer's naming policy, so nothing here needs an attribute.

internal sealed class BotApiResponse<T>
{
    public bool Ok { get; set; }
    public T? Result { get; set; }
    public int? ErrorCode { get; set; }
    public string? Description { get; set; }
    public BotApiResponseParameters? Parameters { get; set; }
}

internal sealed class BotApiResponseParameters
{
    /// <summary>Seconds to wait before repeating the request, on a 429.</summary>
    public int? RetryAfter { get; set; }
}

internal sealed class WireUser
{
    public long Id { get; set; }
    public bool IsBot { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? LanguageCode { get; set; }
}

internal sealed class WireChat
{
    public long Id { get; set; }
    public string Type { get; set; } = "private";
}

internal sealed class WireFile
{
    public string FileId { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public string? FilePath { get; set; }
    public string? MimeType { get; set; }
    public int? Duration { get; set; }
    public string? FileName { get; set; }
}

internal sealed class WireMessage
{
    public long MessageId { get; set; }
    public WireUser? From { get; set; }
    public WireChat Chat { get; set; } = new();

    /// <summary>Unix seconds.</summary>
    public long Date { get; set; }

    public string? Text { get; set; }
    public string? Caption { get; set; }

    public WireFile? Voice { get; set; }
    public WireFile? Audio { get; set; }
    public WireFile? Video { get; set; }
    public WireFile? VideoNote { get; set; }
    public WireFile? Document { get; set; }

    /// <summary>Available sizes of a photo, smallest first.</summary>
    public WireFile[]? Photo { get; set; }

    public WireMessage? ReplyToMessage { get; set; }
}

internal sealed class WireCallbackQuery
{
    public string Id { get; set; } = string.Empty;
    public WireUser From { get; set; } = new();
    public string? Data { get; set; }
    public WireMessage? Message { get; set; }
}

internal sealed class WireUpdate
{
    public long UpdateId { get; set; }
    public WireMessage? Message { get; set; }
    public WireCallbackQuery? CallbackQuery { get; set; }
}

internal sealed class SendMessageRequest
{
    public string ChatId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? ParseMode { get; set; }
    public bool? DisableNotification { get; set; }
    public WireLinkPreviewOptions? LinkPreviewOptions { get; set; }
    public WireReplyParameters? ReplyParameters { get; set; }
    public WireInlineKeyboardMarkup? ReplyMarkup { get; set; }
}

internal sealed class WireLinkPreviewOptions
{
    public bool IsDisabled { get; set; }
}

internal sealed class WireReplyParameters
{
    public long MessageId { get; set; }
}

internal sealed class WireInlineKeyboardMarkup
{
    public List<List<WireInlineKeyboardButton>> InlineKeyboard { get; set; } = [];
}

internal sealed class WireInlineKeyboardButton
{
    public string Text { get; set; } = string.Empty;
    public string? CallbackData { get; set; }
    public string? Url { get; set; }
}

internal sealed class GetUpdatesRequest
{
    public long Offset { get; set; }

    /// <summary>Long-poll timeout, in seconds.</summary>
    public int Timeout { get; set; }
}

internal sealed class AnswerCallbackQueryRequest
{
    public string CallbackQueryId { get; set; } = string.Empty;
    public string? Text { get; set; }
}

internal sealed class GetFileRequest
{
    public string FileId { get; set; } = string.Empty;
}

internal sealed class SetWebhookRequest
{
    public string Url { get; set; } = string.Empty;
    public string SecretToken { get; set; } = string.Empty;
}
