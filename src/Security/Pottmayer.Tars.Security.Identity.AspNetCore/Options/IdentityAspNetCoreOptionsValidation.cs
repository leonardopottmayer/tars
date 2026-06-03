namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

internal static class IdentityAspNetCoreOptionsValidation
{
    public static bool Validate(IdentityAspNetCoreOptions options)
        => options is not null
           && options.Jwt is not null
           && options.Cookie is not null
           && options.RefreshToken is not null
           && options.TokenDelivery is not null
           && options.ApiKey is not null
           && options.Endpoints is not null
           && options.MagicLink is not null;
}
