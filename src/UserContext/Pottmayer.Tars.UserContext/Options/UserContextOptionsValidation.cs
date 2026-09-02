namespace Pottmayer.Tars.UserContext.Options;

/// <summary>
/// Validates <see cref="UserContextOptions"/>.
/// </summary>
internal static class UserContextOptionsValidation
{
    /// <summary>
    /// Validates the given options.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool Validate(UserContextOptions options)
    {
        return options is not null;
    }
}
