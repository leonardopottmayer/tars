namespace Pottmayer.Tars.Core.Localization.AspNetCore.Options;

/// <summary>
/// Validation entry point for <see cref="LocalizationAspNetCoreOptions"/>, wired into the options pipeline by
/// <c>AddTarsLocalizationAspNetCoreOptions</c> and run on application start.
/// </summary>
internal static class LocalizationAspNetCoreOptionsValidation
{
    /// <summary>Validates the bound <see cref="LocalizationAspNetCoreOptions"/> instance.</summary>
    /// <param name="options">The options instance to validate.</param>
    /// <returns><c>true</c> when non-null and <see cref="LocalizationAspNetCoreOptions.IsValid"/>; otherwise <c>false</c>.</returns>
    public static bool Validate(LocalizationAspNetCoreOptions options)
        => options is not null && options.IsValid();
}
