namespace Pottmayer.Tars.Communication.Telegram.Abstractions.Models;

/// <summary>
/// Outcome of a delivered message. <see cref="MessageId"/> is what lets a caller correlate a later
/// reply (<see cref="TelegramIncomingMessage.ReplyToMessageId"/>) back to what it answered.
/// </summary>
public sealed record TelegramSendResult(string ChatId, long MessageId, DateTimeOffset SentAt);
