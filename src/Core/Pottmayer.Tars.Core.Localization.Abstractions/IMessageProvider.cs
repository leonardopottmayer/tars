using System.Globalization;

namespace Pottmayer.Tars.Core.Localization.Abstractions;

/// <summary>Resolves localized messages by key, formatting them with optional arguments.</summary>
public interface IMessageProvider
{
    /// <summary>Resolves a message for the current UI culture.</summary>
    /// <param name="key">The message key to look up.</param>
    /// <param name="fallback">Message used when the key is not found; the key itself is used when null.</param>
    /// <param name="args">Optional arguments applied with <see cref="string.Format(string, object?[])"/>.</param>
    /// <returns>The resolved (and formatted) message.</returns>
    string Get(string key, string? fallback = null, params object[] args);

    /// <summary>Resolves a message for a specific culture.</summary>
    /// <param name="key">The message key to look up.</param>
    /// <param name="culture">The culture to resolve the message for.</param>
    /// <param name="fallback">Message used when the key is not found; the key itself is used when null.</param>
    /// <param name="args">Optional arguments applied with <see cref="string.Format(string, object?[])"/>.</param>
    /// <returns>The resolved (and formatted) message.</returns>
    string Get(string key, CultureInfo culture, string? fallback = null, params object[] args);
}
