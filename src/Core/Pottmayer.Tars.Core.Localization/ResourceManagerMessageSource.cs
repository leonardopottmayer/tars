using System.Globalization;
using System.Resources;
using Pottmayer.Tars.Core.Localization.Abstractions;

namespace Pottmayer.Tars.Core.Localization;

/// <summary><see cref="IMessageSource"/> backed by a .NET <see cref="ResourceManager"/> (.resx resources).</summary>
public sealed class ResourceManagerMessageSource : IMessageSource
{
    private readonly ResourceManager _resourceManager;

    /// <summary>Creates a source over the given <see cref="ResourceManager"/>.</summary>
    /// <param name="resourceManager">The resource manager that holds the localized strings.</param>
    public ResourceManagerMessageSource(ResourceManager resourceManager)
        => _resourceManager = resourceManager;

    /// <inheritdoc/>
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
