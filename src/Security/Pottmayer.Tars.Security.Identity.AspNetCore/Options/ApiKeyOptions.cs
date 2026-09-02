namespace Pottmayer.Tars.Security.Identity.AspNetCore.Options;

/// <summary>
/// API key authentication settings.
/// </summary>
public sealed class ApiKeyOptions
{
    /// <summary>The authentication scheme name registered for API key authentication.</summary>
    public string SchemeName { get; init; } = "ApiKey";

    /// <summary>The HTTP header the API key is read from.</summary>
    public string HeaderName { get; init; } = "X-Api-Key";

    /// <summary>An optional query string parameter the API key may also be read from.</summary>
    public string? QueryParameterName { get; init; }
}
