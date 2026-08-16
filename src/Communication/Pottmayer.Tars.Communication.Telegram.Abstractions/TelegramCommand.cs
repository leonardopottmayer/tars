using System.Diagnostics.CodeAnalysis;

namespace Pottmayer.Tars.Communication.Telegram.Abstractions;

/// <summary>
/// Parses the Bot API's slash-command syntax. This is transport grammar, not application meaning:
/// it tells the caller <em>that</em> a message is <c>/start</c> with an argument, never what
/// <c>/start</c> should do.
/// </summary>
public static class TelegramCommand
{
    /// <summary>
    /// Recognizes <c>/command</c>, <c>/command argument</c> and the group form <c>/command@botname</c>.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="command">The command, lowercased and without the leading slash or bot suffix.</param>
    /// <param name="argument">Everything after the first space, trimmed. Null when there is none.</param>
    /// <returns>False when the text is not a command, in which case both outputs are null.</returns>
    public static bool TryParse(
        string? text,
        [NotNullWhen(true)] out string? command,
        out string? argument)
    {
        command = null;
        argument = null;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.TrimStart();
        if (trimmed.Length < 2 || trimmed[0] != '/')
            return false;

        var spaceIndex = trimmed.IndexOf(' ', StringComparison.Ordinal);

        var token = spaceIndex < 0 ? trimmed[1..] : trimmed[1..spaceIndex];
        var rest = spaceIndex < 0 ? null : trimmed[(spaceIndex + 1)..].Trim();

        // In groups Telegram appends the addressed bot: "/start@my_bot".
        var atIndex = token.IndexOf('@', StringComparison.Ordinal);
        if (atIndex >= 0)
            token = token[..atIndex];

        if (token.Length == 0)
            return false;

        command = token.ToLowerInvariant();
        argument = string.IsNullOrEmpty(rest) ? null : rest;
        return true;
    }
}
