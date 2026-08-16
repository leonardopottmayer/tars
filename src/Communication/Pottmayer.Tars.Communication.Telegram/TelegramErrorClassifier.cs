namespace Pottmayer.Tars.Communication.Telegram;

/// <summary>
/// Decides whether a Bot API failure can ever succeed on a retry. This is the one judgement the
/// building block makes on the caller's behalf, because getting it wrong means either retrying five
/// times against a blocked bot or dead-lettering a message that a 500 would have delivered.
/// </summary>
internal static class TelegramErrorClassifier
{
    /// <summary>
    /// Every 4xx except 429 is permanent: the request itself is the problem, so repeating it verbatim
    /// reproduces the failure. That covers <c>400 chat not found</c>, <c>401</c> for a bad token,
    /// <c>403 bot was blocked by the user</c> and <c>409 Conflict</c> from a webhook competing with
    /// long polling. 429 is rate limiting and 5xx is Telegram having a bad minute — both transient.
    /// </summary>
    internal static bool IsPermanent(int? errorCode)
        => errorCode is >= 400 and < 500 && errorCode != 429;
}
