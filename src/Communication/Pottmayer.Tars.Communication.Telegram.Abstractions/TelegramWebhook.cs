using System.Security.Cryptography;
using System.Text;

namespace Pottmayer.Tars.Communication.Telegram.Abstractions;

/// <summary>Helpers for hosting a Telegram webhook endpoint.</summary>
public static class TelegramWebhook
{
    /// <summary>
    /// The header Telegram echoes the configured secret token in, on every webhook request.
    /// </summary>
    public const string SecretTokenHeaderName = "X-Telegram-Bot-Api-Secret-Token";

    /// <summary>
    /// Compares the header value against the configured secret in constant time. The endpoint is
    /// anonymous, so this comparison is the only thing authenticating the caller — a length-dependent
    /// or short-circuiting comparison leaks the secret one byte at a time.
    /// </summary>
    /// <returns>False when either value is missing, so a misconfigured endpoint fails closed.</returns>
    public static bool IsValidSecretToken(string? headerValue, string? expectedSecret)
    {
        if (string.IsNullOrEmpty(headerValue) || string.IsNullOrEmpty(expectedSecret))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(headerValue),
            Encoding.UTF8.GetBytes(expectedSecret));
    }
}
