using System.Globalization;
using Microsoft.Extensions.Localization;
using Pottmayer.Tars.Core.Localization.Abstractions;

namespace Pottmayer.Tars.Core.Localization.AspNetCore;

/// <summary>
/// <see cref="IMessageSource"/> that adapts ASP.NET Core's <see cref="IStringLocalizer"/> infrastructure,
/// resolving keys against a resource identified by base name and location.
/// </summary>
public sealed class StringLocalizerMessageSource : IMessageSource
{
    private readonly IStringLocalizerFactory _factory;
    private readonly string _baseName;
    private readonly string _location;

    /// <summary>Creates a source over the given localizer factory and resource coordinates.</summary>
    /// <param name="factory">The string localizer factory.</param>
    /// <param name="baseName">The resource base name (typically the resource type's full name).</param>
    /// <param name="location">The resource location (typically the assembly name).</param>
    public StringLocalizerMessageSource(
        IStringLocalizerFactory factory,
        string baseName,
        string location)
    {
        _factory = factory;
        _baseName = baseName;
        _location = location;
    }

    /// <inheritdoc/>
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
