namespace Pottmayer.Tars.Core.Localization.AspNetCore.Options;

/// <summary>Options for ASP.NET Core request localization, bound from configuration.</summary>
public sealed class LocalizationAspNetCoreOptions
{
    /// <summary>Default configuration section these options bind from (<c>Tars:Localization</c>).</summary>
    public const string SectionName = "Tars:Localization";

    /// <summary>Message reported when validation fails on application start.</summary>
    public const string ValidationErrorMessage = "Invalid LocalizationAspNetCoreOptions.";

    /// <summary>Culture used when a request specifies none. Defaults to <c>en-US</c>.</summary>
    public string DefaultCulture { get; init; } = "en-US";

    /// <summary>Cultures the application supports for both formatting and UI. Defaults to <c>en-US</c>.</summary>
    public IReadOnlyList<string> SupportedCultures { get; init; } = ["en-US"];

    /// <summary>
    /// Returns <c>true</c> when the options are internally consistent: a default culture is present and at
    /// least one supported culture is configured.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(DefaultCulture))
            return false;

        if (SupportedCultures is null || SupportedCultures.Count == 0)
            return false;

        return true;
    }
}
