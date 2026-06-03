using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.AspNetCore.Metadata;
using Pottmayer.Tars.Web.Http.AspNetCore.Options;
using Pottmayer.Tars.Web.Http.Options;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Filters;

public sealed class ResponseWrapperEndpointFilter : IEndpointFilter
{
    private readonly IOptionsMonitor<WebHttpOptions> _optionsMonitor;
    private readonly IOptionsMonitor<WebHttpAspNetCoreOptions> _aspNetCoreOptionsMonitor;
    private readonly IWrapDecisionService _decisionService;

    public ResponseWrapperEndpointFilter(
        IOptionsMonitor<WebHttpOptions> optionsMonitor,
        IOptionsMonitor<WebHttpAspNetCoreOptions> aspNetCoreOptionsMonitor,
        IWrapDecisionService decisionService)
    {
        _optionsMonitor = optionsMonitor;
        _aspNetCoreOptionsMonitor = aspNetCoreOptionsMonitor;
        _decisionService = decisionService;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled || result is null)
            return result;

        var endpoint = context.HttpContext.GetEndpoint();
        var optIn      = endpoint?.Metadata.GetMetadata<ResponseWrapperMetadata>() is not null;
        var disabled   = endpoint?.Metadata.GetMetadata<DisableResponseWrapperMetadata>() is not null;
        var aspOptions = _aspNetCoreOptionsMonitor.CurrentValue;

        var shouldWrap = _decisionService.ShouldWrap(new WrapDecisionContext
        {
            WrappingEnabled       = options.Enabled,
            IsAlreadyWrapped      = result is IHttpResponse,
            IsFileOrStream        = result is IResult,
            IsExplicitDisabled    = disabled,
            IsExplicitEnabled     = optIn,
            ControllersDefaultMode = aspOptions.ControllersDefaultMode,
            MinimalApiOptIn       = aspOptions.MinimalApisEnabledByDefault || optIn
        });

        if (!shouldWrap)
            return result;

        var traceId = options.IncludeTraceId ? ResponseWrapperResultFilter.ResolveTraceId(context.HttpContext) : null;
        var wrapped = new HttpResponse<object?> { Success = true, Data = result, TraceId = traceId };
        return Results.Json(wrapped, statusCode: 200);
    }
}
