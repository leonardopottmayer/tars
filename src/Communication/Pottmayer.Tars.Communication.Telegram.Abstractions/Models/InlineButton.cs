using System.Text;

namespace Pottmayer.Tars.Communication.Telegram.Abstractions.Models;

/// <summary>
/// One inline-keyboard button. Either a callback button, which posts <see cref="CallbackData"/> back
/// to the bot, or a link button, which just opens a URL and never reaches the bot. Build them with
/// <see cref="Callback"/> and <see cref="Link"/>.
/// </summary>
public sealed record InlineButton
{
    /// <summary>
    /// The Bot API limit for <c>callback_data</c>, in UTF-8 bytes. It is small on purpose: the field
    /// is meant to hold a key, not a payload.
    /// </summary>
    public const int MaxCallbackDataBytes = 64;

    private InlineButton(string label, string? callbackData, Uri? url)
    {
        Label = label;
        CallbackData = callbackData;
        Url = url;
    }

    /// <summary>The text shown on the button.</summary>
    public string Label { get; }

    /// <summary>Opaque data posted back as a callback query. Null for a URL button.</summary>
    public string? CallbackData { get; }

    /// <summary>The link to open. Null for a callback button.</summary>
    public Uri? Url { get; }

    /// <summary>
    /// A button that posts <paramref name="callbackData"/> back to the bot when tapped.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The label is empty, or the data is empty or exceeds <see cref="MaxCallbackDataBytes"/> UTF-8
    /// bytes. Telegram rejects an oversized value at send time; failing here turns a runtime surprise
    /// into a caller bug, which is why storing an id and resolving it is the only workable design.
    /// </exception>
    public static InlineButton Callback(string label, string callbackData)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Button label must not be empty.", nameof(label));

        if (string.IsNullOrEmpty(callbackData))
            throw new ArgumentException("Callback data must not be empty.", nameof(callbackData));

        var byteCount = Encoding.UTF8.GetByteCount(callbackData);
        if (byteCount > MaxCallbackDataBytes)
        {
            throw new ArgumentException(
                $"Callback data is {byteCount} UTF-8 bytes; Telegram allows at most {MaxCallbackDataBytes}. " +
                "Store the payload and use its id as the callback data.",
                nameof(callbackData));
        }

        return new InlineButton(label, callbackData, url: null);
    }

    /// <summary>A button that opens <paramref name="url"/>. It produces no callback query.</summary>
    public static InlineButton Link(string label, Uri url)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Button label must not be empty.", nameof(label));

        ArgumentNullException.ThrowIfNull(url);

        return new InlineButton(label, callbackData: null, url);
    }
}
