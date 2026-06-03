namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

public sealed class ApiKeyOptions
{
    public string SchemeName { get; init; } = "ApiKey";
    public string HeaderName { get; init; } = "X-Api-Key";
    public string? QueryParameterName { get; init; }
}
