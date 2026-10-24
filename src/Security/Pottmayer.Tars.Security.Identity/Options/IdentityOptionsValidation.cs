using System.Text;

namespace Pottmayer.Tars.Security.Identity.Options;

/// <summary>
/// Validates <see cref="SecurityIdentityOptions"/>.
/// </summary>
internal static class IdentityOptionsValidation
{
    /// <summary>Minimum JWT signing key length in UTF-8 bytes (256 bits, required by HMAC-SHA256).</summary>
    public const int MinSigningKeyBytes = 32;

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

        // HMAC-SHA256 needs a key of at least 256 bits; a shorter one only fails at the first token issuance.
        if (Encoding.UTF8.GetByteCount(options.Jwt.SigningKey) < MinSigningKeyBytes)
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
