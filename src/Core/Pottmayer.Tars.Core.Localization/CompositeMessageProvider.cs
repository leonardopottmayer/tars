using System.Globalization;
using Pottmayer.Tars.Core.Localization.Abstractions;

namespace Pottmayer.Tars.Core.Localization;

/// <summary>
/// <see cref="IMessageProvider"/> that queries an ordered list of <see cref="IMessageSource"/>s, returning
/// the first match and falling back to the supplied fallback (or the key) when none has the message.
/// </summary>
public sealed class CompositeMessageProvider : IMessageProvider
{
    private readonly IReadOnlyList<IMessageSource> _sources;

    /// <summary>Creates a provider over the given sources, queried in order.</summary>
    /// <param name="sources">The message sources, in priority order.</param>
    public CompositeMessageProvider(IEnumerable<IMessageSource> sources)
        => _sources = sources.ToList();

    /// <inheritdoc/>
    public string Get(string key, string? fallback = null, params object[] args)
        => Resolve(key, CultureInfo.CurrentUICulture, fallback, args);

    /// <inheritdoc/>
    public string Get(string key, CultureInfo culture, string? fallback = null, params object[] args)
        => Resolve(key, culture, fallback, args);

    /// <summary>Returns the first source's match for the key, else the fallback (or key), formatted with args.</summary>
    private string Resolve(string key, CultureInfo culture, string? fallback, object[] args)
    {
        foreach (var source in _sources)
        {
            var value = source.TryGet(key, culture);
            if (value is not null)
                return Format(value, args);
        }

        var message = fallback ?? key;
        return Format(message, args);
    }

    /// <summary>Applies <see cref="string.Format(string, object?[])"/> when args are present; otherwise returns the message unchanged.</summary>
    private static string Format(string message, object[] args)
        => args.Length > 0 ? string.Format(message, args) : message;
}
