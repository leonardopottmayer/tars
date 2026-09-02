namespace Pottmayer.Tars.Security.Identity.Options;

/// <summary>
/// Validates <see cref="SecurityIdentityOptions"/>.
/// </summary>
internal static class IdentityOptionsValidation
{
    /// <summary>
    /// Validates the given options.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool Validate(SecurityIdentityOptions options)
    {
        if (options is null)
            return false;

        if (options.Jwt is null || string.IsNullOrWhiteSpace(options.Jwt.SigningKey))
            return false;

        if (options.Jwt.AccessTokenLifetime <= TimeSpan.Zero)
            return false;

        if (options.RefreshToken is null)
            return false;

        if (options.TokenDelivery is null)
            return false;

        if (options.Revocation is null)
            return false;

        return true;
    }
}
