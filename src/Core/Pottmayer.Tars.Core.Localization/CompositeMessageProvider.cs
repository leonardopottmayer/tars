using System.Globalization;
using Pottmayer.Tars.Core.Localization.Abstractions;

namespace Pottmayer.Tars.Core.Localization;

public sealed class CompositeMessageProvider : IMessageProvider
{
    private readonly IReadOnlyList<IMessageSource> _sources;

    public CompositeMessageProvider(IEnumerable<IMessageSource> sources)
        => _sources = sources.ToList();

    public string Get(string key, string? fallback = null, params object[] args)
        => Resolve(key, CultureInfo.CurrentUICulture, fallback, args);

    public string Get(string key, CultureInfo culture, string? fallback = null, params object[] args)
        => Resolve(key, culture, fallback, args);

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

    private static string Format(string message, object[] args)
        => args.Length > 0 ? string.Format(message, args) : message;
}
