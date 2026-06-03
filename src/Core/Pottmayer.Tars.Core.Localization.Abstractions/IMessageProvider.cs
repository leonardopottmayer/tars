using System.Globalization;

namespace Pottmayer.Tars.Core.Localization.Abstractions;

public interface IMessageProvider
{
    string Get(string key, string? fallback = null, params object[] args);
    string Get(string key, CultureInfo culture, string? fallback = null, params object[] args);
}
