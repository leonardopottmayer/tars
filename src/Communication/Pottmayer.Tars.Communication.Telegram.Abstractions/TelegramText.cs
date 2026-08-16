using System.Text;

namespace Pottmayer.Tars.Communication.Telegram.Abstractions;

/// <summary>
/// Escaping helpers for the Bot API's text formats. Any text that came from a user or a template and
/// is sent with a parse mode must go through these — an unescaped <c>.</c> or <c>-</c> is enough for
/// Telegram to reject the whole message with <c>400 Bad Request</c>.
/// </summary>
public static class TelegramText
{
    /// <summary>
    /// The characters MarkdownV2 reserves. Telegram requires every one of them to be escaped in plain
    /// text, including the ones that look harmless.
    /// </summary>
    private const string MarkdownV2Reserved = @"_*[]()~`>#+-=|{}.!\";

    /// <summary>
    /// Escapes text for <see cref="Models.TelegramParseMode.MarkdownV2"/> by prefixing every reserved
    /// character with a backslash.
    /// </summary>
    public static string EscapeMarkdownV2(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length + 16);
        foreach (var c in text)
        {
            if (MarkdownV2Reserved.Contains(c))
                builder.Append('\\');

            builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Escapes text for <see cref="Models.TelegramParseMode.Html"/>. The Bot API's HTML subset only
    /// requires <c>&amp;</c>, <c>&lt;</c> and <c>&gt;</c> to be replaced.
    /// </summary>
    public static string EscapeHtml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }
}
