using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.Options;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Filters;

public sealed class TarsExceptionFilter : IExceptionFilter
{
    private readonly IHttpErrorMapper _errorMapper;
    private readonly IOptionsMonitor<WebHttpOptions> _optionsMonitor;
    private readonly ILogger<TarsExceptionFilter> _logger;

    public TarsExceptionFilter(
        IHttpErrorMapper errorMapper,
        IOptionsMonitor<WebHttpOptions> optionsMonitor,
        ILogger<TarsExceptionFilter> logger)
    {
        _errorMapper = errorMapper;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception caught by TarsExceptionFilter");

        var traceId = _optionsMonitor.CurrentValue.IncludeTraceId
            ? ResponseWrapperResultFilter.ResolveTraceId(context.HttpContext)
            : null;

        if (context.Exception is IExpectedException expected)
        {
            var first = expected.Errors.FirstOrDefault();
            if (first is not null)
            {
                var statusCode = _errorMapper.MapToStatusCode(first.Type);
                context.Result = new ObjectResult(WithTraceId(_errorMapper.Map(first), traceId)) { StatusCode = statusCode };
            }
            else
            {
                context.Result = new ObjectResult(WithTraceId(_errorMapper.Map(context.Exception), traceId)) { StatusCode = 400 };
            }
        }
        else
        {
            context.Result = new ObjectResult(WithTraceId(_errorMapper.Map(context.Exception), traceId)) { StatusCode = 500 };
        }

        context.ExceptionHandled = true;
    }

    private static HttpErrorResponse WithTraceId(IHttpErrorResponse response, string? traceId)
        => new()
        {
            Success      = response.Success,
            ErrorCode    = response.ErrorCode,
            ErrorMessage = response.ErrorMessage,
            FieldErrors  = response.FieldErrors,
            TraceId      = traceId
        };
}
