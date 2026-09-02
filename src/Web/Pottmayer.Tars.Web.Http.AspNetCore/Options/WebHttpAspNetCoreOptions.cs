using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Options;

/// <summary>
/// Configures ASP.NET Core-specific HTTP response behavior.
/// </summary>
public sealed class WebHttpAspNetCoreOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "Tars:Web:Http:AspNetCore";
    /// <summary>Gets the error message used when validation fails.</summary>
    public const string ValidationErrorMessage = "Invalid WebHttpAspNetCoreOptions.";

    /// <summary>Gets the default wrapping behavior for MVC controllers.</summary>
    public ControllersWrappingMode ControllersDefaultMode { get; set; } = ControllersWrappingMode.WrapAll;
    /// <summary>Gets whether Minimal API endpoints are wrapped when they have no explicit metadata.</summary>
    public bool MinimalApisEnabledByDefault { get; set; } = false;

    /// <summary>Determines whether the options are valid.</summary>
    /// <returns>Whether the controllers wrapping mode is a defined value.</returns>
    public bool IsValid() => Enum.IsDefined(ControllersDefaultMode);
}
