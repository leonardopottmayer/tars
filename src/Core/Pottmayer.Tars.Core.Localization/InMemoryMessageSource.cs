using System.Globalization;
using Pottmayer.Tars.Core.Localization.Abstractions;

namespace Pottmayer.Tars.Core.Localization;

public sealed class InMemoryMessageSource : IMessageSource
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _messages;

    public InMemoryMessageSource(IDictionary<string, IDictionary<string, string>> messages)
    {
        _messages = messages.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(kv.Value,
                StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

    public string? TryGet(string key, CultureInfo culture)
    {
        if (_messages.TryGetValue(culture.Name, out var exact) && exact.TryGetValue(key, out var msg))
            return msg;

        if (!culture.IsNeutralCulture)
        {
            var neutral = culture.Parent;
            if (_messages.TryGetValue(neutral.TwoLetterISOLanguageName, out var neutralDict)
                && neutralDict.TryGetValue(key, out var neutralMsg))
                return neutralMsg;
        }

        if (_messages.TryGetValue("en", out var en) && en.TryGetValue(key, out var enMsg))
            return enMsg;

        return null;
    }
}
