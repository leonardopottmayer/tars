namespace Pottmayer.Tars.Communication.Telegram.Abstractions.Models;

/// <summary>
/// An inline keyboard attached to a message: rows of buttons, rendered under the text.
/// </summary>
public sealed record InlineKeyboard(IReadOnlyList<IReadOnlyList<InlineButton>> Rows)
{
    /// <summary>A keyboard with every button on its own row — the readable default for two or three actions.</summary>
    public static InlineKeyboard Stacked(params InlineButton[] buttons)
        => new([.. buttons.Select(IReadOnlyList<InlineButton> (b) => [b])]);

    /// <summary>A keyboard with every button side by side on a single row.</summary>
    public static InlineKeyboard SingleRow(params InlineButton[] buttons)
        => new([buttons]);
}
