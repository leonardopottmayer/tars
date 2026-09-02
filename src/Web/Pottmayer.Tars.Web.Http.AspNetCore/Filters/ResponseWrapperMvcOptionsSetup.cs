using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Filters;

/// <summary>
/// Adds <see cref="ResponseWrapperResultFilter"/> to MVC options.
/// </summary>
public sealed class ResponseWrapperMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    /// <inheritdoc/>
    public void Configure(MvcOptions options)
        => options.Filters.AddService<ResponseWrapperResultFilter>();
}
