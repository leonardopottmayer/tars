namespace Pottmayer.Tars.Web.Http.Options;

/// <summary>
/// Validates <see cref="WebHttpOptions"/> instances bound from configuration.
/// </summary>
internal static class WebHttpOptionsValidation
{
    /// <summary>Validates the supplied options.</summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>Whether the options are non-null and valid.</returns>
    public static bool Validate(WebHttpOptions options)
        => options is not null && options.IsValid();
}
