namespace Pottmayer.Tars.Security.Identity.Options;

internal static class IdentityOptionsValidation
{
    public static bool Validate(IdentityOptions options)
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
