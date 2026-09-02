namespace Pottmayer.Tars.Web.Http.AspNetCore.Options;

/// <summary>
/// Validates <see cref="WebHttpAspNetCoreOptions"/> instances bound from configuration.
/// </summary>
internal static class WebHttpAspNetCoreOptionsValidation
{
    /// <summary>Validates the supplied options.</summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>Whether the options are non-null and valid.</returns>
    public static bool Validate(WebHttpAspNetCoreOptions options)
        => options is not null && options.IsValid();
}
