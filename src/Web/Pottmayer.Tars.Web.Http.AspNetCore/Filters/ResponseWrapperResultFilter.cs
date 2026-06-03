using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.AspNetCore.Attributes;
using Pottmayer.Tars.Web.Http.AspNetCore.Options;
using Pottmayer.Tars.Web.Http.Options;
using System.Diagnostics;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Filters;

public sealed class ResponseWrapperResultFilter : IAsyncResultFilter
{
    private readonly IOptionsMonitor<WebHttpOptions> _optionsMonitor;
    private readonly IOptionsMonitor<WebHttpAspNetCoreOptions> _aspNetCoreOptionsMonitor;
    private readonly IWrapDecisionService _decisionService;

    public ResponseWrapperResultFilter(
        IOptionsMonitor<WebHttpOptions> optionsMonitor,
        IOptionsMonitor<WebHttpAspNetCoreOptions> aspNetCoreOptionsMonitor,
        IWrapDecisionService decisionService)
    {
        _optionsMonitor = optionsMonitor;
        _aspNetCoreOptionsMonitor = aspNetCoreOptionsMonitor;
        _decisionService = decisionService;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
        {
            await next();
            return;
        }

        var result = context.Result;

        if (IsSkippedResultType(result))
        {
            await next();
            return;
        }

        if (result is not ObjectResult objectResult)
        {
            if (result is EmptyResult or NoContentResult)
            {
                if (ShouldWrapForController(context, isAlreadyWrapped: false))
                {
                    var traceId = options.IncludeTraceId ? ResolveTraceId(context.HttpContext) : null;
                    context.Result = new ObjectResult(
                        new HttpResponse<object?> { Success = true, Data = null, TraceId = traceId })
                    {
                        StatusCode = result is NoContentResult ? 204 : 200
                    };
                }
            }

            await next();
            return;
        }

        var value = objectResult.Value;

        if (value is IHttpResponse || !ShouldWrapForController(context, isAlreadyWrapped: value is IHttpResponse))
        {
            await next();
            return;
        }

        var statusCode = objectResult.StatusCode ?? 200;
        var responseTraceId = options.IncludeTraceId ? ResolveTraceId(context.HttpContext) : null;

        objectResult.Value = statusCode is >= 400
            ? BuildErrorResponse(value, responseTraceId)
            : (object)new HttpResponse<object?> { Success = true, Data = value, TraceId = responseTraceId };

        await next();
    }

    private bool ShouldWrapForController(ResultExecutingContext context, bool isAlreadyWrapped)
    {
        var options = _optionsMonitor.CurrentValue;
        var aspNetCoreOptions = _aspNetCoreOptionsMonitor.CurrentValue;

        var isDisabled = context.ActionDescriptor.FilterDescriptors
            .Any(f => f.Filter is DisableResponseWrapperAttribute);
        var isEnabled = context.ActionDescriptor.FilterDescriptors
            .Any(f => f.Filter is EnableResponseWrapperAttribute);

        return _decisionService.ShouldWrap(new WrapDecisionContext
        {
            WrappingEnabled       = options.Enabled,
            IsAlreadyWrapped      = isAlreadyWrapped,
            IsFileOrStream        = false,
            IsExplicitDisabled    = isDisabled,
            IsExplicitEnabled     = isEnabled,
            ControllersDefaultMode = aspNetCoreOptions.ControllersDefaultMode,
            MinimalApiOptIn       = false
        });
    }

    private static HttpErrorResponse BuildErrorResponse(object? value, string? traceId)
    {
        if (value is ValidationProblemDetails validation)
        {
            var fieldErrors = validation.Errors?
                .SelectMany(kv => kv.Value.Select(msg => (IHttpFieldError)new HttpFieldError(kv.Key, msg)))
                .ToList();

            return new HttpErrorResponse
            {
                Success      = false,
                ErrorCode    = "VALIDATION_ERROR",
                ErrorMessage = validation.Detail ?? validation.Title,
                FieldErrors  = fieldErrors,
                TraceId      = traceId
            };
        }

        if (value is ProblemDetails pd)
        {
            var code = pd.Extensions is not null && pd.Extensions.TryGetValue("code", out var c)
                ? c?.ToString()
                : pd.Title;

            return new HttpErrorResponse
            {
                Success      = false,
                ErrorCode    = code,
                ErrorMessage = pd.Detail ?? pd.Title,
                TraceId      = traceId
            };
        }

        return new HttpErrorResponse
        {
            Success      = false,
            ErrorCode    = "ERROR",
            ErrorMessage = value?.ToString(),
            TraceId      = traceId
        };
    }

    internal static string ResolveTraceId(HttpContext httpContext)
        => Activity.Current?.TraceId.ToHexString() is { Length: > 0 } id
            ? id
            : httpContext.TraceIdentifier;

    private static bool IsSkippedResultType(IActionResult result)
        => result is FileResult
            or RedirectResult
            or LocalRedirectResult
            or RedirectToActionResult
            or RedirectToRouteResult
            or RedirectToPageResult
            or ChallengeResult
            or SignInResult
            or SignOutResult
            or ForbidResult
            or UnauthorizedResult
            or UnauthorizedObjectResult;
}
