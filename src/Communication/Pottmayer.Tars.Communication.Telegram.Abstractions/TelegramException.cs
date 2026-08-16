namespace Pottmayer.Tars.Communication.Telegram.Abstractions;

/// <summary>
/// A Bot API call failed. <see cref="IsPermanent"/> is the field callers act on: a permanent failure
/// will fail identically forever (the chat is gone, the bot is blocked, the request is malformed), so
/// retrying it only burns attempts against a queue.
/// </summary>
public sealed class TelegramException : Exception
{
    public TelegramException(
        string method,
        string message,
        bool isPermanent,
        int? errorCode = null,
        TimeSpan? retryAfter = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Method = method;
        IsPermanent = isPermanent;
        ErrorCode = errorCode;
        RetryAfter = retryAfter;
    }

    /// <summary>The Bot API method that failed, e.g. <c>sendMessage</c>.</summary>
    public string Method { get; }

    /// <summary>
    /// True when retrying cannot succeed. The caller should stop and record the failure — and, for a
    /// blocked or deleted chat, stop using that address at all.
    /// </summary>
    public bool IsPermanent { get; }

    /// <summary>The Bot API <c>error_code</c>, when the failure came from Telegram rather than the wire.</summary>
    public int? ErrorCode { get; }

    /// <summary>
    /// How long Telegram asked the caller to wait, from <c>parameters.retry_after</c> on a 429. Null
    /// when it did not say.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
