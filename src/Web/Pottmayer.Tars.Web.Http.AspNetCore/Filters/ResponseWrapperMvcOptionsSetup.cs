using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Filters;

public sealed class ResponseWrapperMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    public void Configure(MvcOptions options)
        => options.Filters.AddService<ResponseWrapperResultFilter>();
}
