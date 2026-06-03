namespace Pottmayer.Tars.Web.Http.Options;

public static class WebHttpOptionsValidation
{
    public static bool Validate(WebHttpOptions options)
        => options.IsValid();
}
