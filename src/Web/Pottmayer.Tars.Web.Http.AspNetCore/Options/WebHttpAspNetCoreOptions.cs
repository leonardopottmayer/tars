using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.Options;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Options;

public sealed class WebHttpAspNetCoreOptions
{
    public const string SectionName = WebHttpOptions.SectionName;
    public const string ValidationErrorMessage = "Invalid WebHttpAspNetCoreOptions.";

    public ControllersWrappingMode ControllersDefaultMode { get; init; } = ControllersWrappingMode.WrapAll;
    public bool MinimalApisEnabledByDefault { get; init; } = false;
}
