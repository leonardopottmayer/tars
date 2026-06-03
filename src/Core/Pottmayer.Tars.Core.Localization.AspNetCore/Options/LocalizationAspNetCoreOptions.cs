namespace Pottmayer.Tars.Core.Localization.AspNetCore.Options;

public sealed class LocalizationAspNetCoreOptions
{
    public const string SectionName = "Tars:Localization";
    public const string ValidationErrorMessage = "Invalid LocalizationAspNetCoreOptions.";

    public string DefaultCulture { get; init; } = "en-US";
    public IReadOnlyList<string> SupportedCultures { get; init; } = ["en-US"];
}
