using System.Globalization;

namespace Pottmayer.Tars.Core.Localization.Abstractions;

/// <summary>A backing store of localized messages that a provider queries by key and culture.</summary>
public interface IMessageSource
{
    /// <summary>Looks up a message by key for the given culture.</summary>
    /// <param name="key">The message key to look up.</param>
    /// <param name="culture">The culture to resolve the message for.</param>
    /// <returns>The message, or <c>null</c> when this source has no entry for the key.</returns>
    string? TryGet(string key, CultureInfo culture);
}
