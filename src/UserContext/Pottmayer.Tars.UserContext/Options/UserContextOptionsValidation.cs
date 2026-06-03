namespace Pottmayer.Tars.UserContext.Options;

internal static class UserContextOptionsValidation
{
    public static bool Validate(UserContextOptions options)
    {
        return options is not null;
    }
}
