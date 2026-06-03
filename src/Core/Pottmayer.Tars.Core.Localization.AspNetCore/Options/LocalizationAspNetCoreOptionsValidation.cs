namespace Pottmayer.Tars.Core.Localization.AspNetCore.Options;

public static class LocalizationAspNetCoreOptionsValidation
{
    public static bool Validate(LocalizationAspNetCoreOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DefaultCulture))
            return false;

        if (options.SupportedCultures is null || options.SupportedCultures.Count == 0)
            return false;

        return true;
    }
}
