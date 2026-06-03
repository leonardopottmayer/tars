using System.Globalization;
using System.Resources;
using Pottmayer.Tars.Core.Localization.Abstractions;

namespace Pottmayer.Tars.Core.Localization;

public sealed class ResourceManagerMessageSource : IMessageSource
{
    private readonly ResourceManager _resourceManager;

    public ResourceManagerMessageSource(ResourceManager resourceManager)
        => _resourceManager = resourceManager;

    public string? TryGet(string key, CultureInfo culture)
    {
        try
        {
            return _resourceManager.GetString(key, culture);
        }
        catch (MissingManifestResourceException)
        {
            return null;
        }
    }
}
