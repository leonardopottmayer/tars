namespace Pottmayer.Tars.Web.Http.Options;

/// <summary>
/// Configures framework-agnostic HTTP response behavior.
/// </summary>
public sealed class WebHttpOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "Tars:Web:Http";
    /// <summary>Gets the error message used when validation fails.</summary>
    public const string ValidationErrorMessage = "Invalid WebHttpOptions.";

    /// <summary>Gets whether automatic response wrapping is enabled.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Gets whether generated envelopes include the current trace ID.</summary>
    public bool IncludeTraceId { get; set; } = false;

    /// <summary>Determines whether the options are valid.</summary>
    /// <returns><c>true</c> because all current values are valid.</returns>
    public bool IsValid() => true;
}
