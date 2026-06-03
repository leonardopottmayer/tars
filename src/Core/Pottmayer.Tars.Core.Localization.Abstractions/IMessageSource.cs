using System.Globalization;

namespace Pottmayer.Tars.Core.Localization.Abstractions;

public interface IMessageSource
{
    string? TryGet(string key, CultureInfo culture);
}
