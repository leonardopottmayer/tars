using System.Globalization;
using Microsoft.Extensions.Localization;
using Pottmayer.Tars.Core.Localization.Abstractions;

namespace Pottmayer.Tars.Core.Localization.AspNetCore;

public sealed class StringLocalizerMessageSource : IMessageSource
{
    private readonly IStringLocalizerFactory _factory;
    private readonly string _baseName;
    private readonly string _location;

    public StringLocalizerMessageSource(
        IStringLocalizerFactory factory,
        string baseName,
        string location)
    {
        _factory = factory;
        _baseName = baseName;
        _location = location;
    }

    public string? TryGet(string key, CultureInfo culture)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = culture;
            var localizer = _factory.Create(_baseName, _location);
            var result = localizer[key];
            return result.ResourceNotFound ? null : result.Value;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
