namespace Pottmayer.Tars.Security.DataProtection.Options;

/// <summary>
/// Validates <see cref="DataProtectionOptions"/>.
/// </summary>
internal static class DataProtectionOptionsValidation
{
    /// <summary>
    /// Validates the given options.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool Validate(DataProtectionOptions options)
    {
        if (options is null)
            return false;

        if (string.IsNullOrWhiteSpace(options.ActiveKeyVersion))
            return false;

        if (options.Keys.Count == 0)
            return false;

        if (!options.Keys.ContainsKey(options.ActiveKeyVersion))
            return false;

        return true;
    }
}
