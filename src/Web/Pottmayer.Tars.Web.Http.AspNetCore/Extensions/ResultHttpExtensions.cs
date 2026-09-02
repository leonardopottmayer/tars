using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Extensions;

/// <summary>
/// Converts Tars results to MVC actions and Minimal API results.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>Converts a successful or failed generic result to an MVC action result.</summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">The error mapper.</param>
    /// <returns>The MVC action result.</returns>
    public static IActionResult ToActionResult<T>(this Result<T> result, IHttpErrorMapper mapper)
        where T : notnull
    {
        var traceId = ResolveTraceId();

        if (result.IsSuccess)
            return new OkObjectResult(new HttpResponse<T> { Success = true, Data = result.Value, TraceId = traceId });

        return BuildErrorActionResult(result.Errors, mapper, traceId);
    }

    public static IActionResult ToActionResult(this Result result, IHttpErrorMapper mapper)
    {
        var traceId = ResolveTraceId();

        if (result.IsSuccess)
            return new OkObjectResult(new HttpResponse<object?> { Success = true, Data = null, TraceId = traceId });

        return BuildErrorActionResult(result.Errors, mapper, traceId);
    }

    private static IActionResult BuildErrorActionResult(
        IReadOnlyList<Error> errors, IHttpErrorMapper mapper, string? traceId)
    {
        var first = errors.FirstOrDefault();
        if (first is null)
            return new ObjectResult(new HttpErrorResponse { Success = false, ErrorCode = "ERROR", TraceId = traceId })
            {
                StatusCode = 500
            };

        var statusCode = mapper.MapToStatusCode(first.Type);
        var mapped     = mapper.Map(first);

        var response = new HttpErrorResponse
        {
            Success      = false,
            ErrorCode    = mapped.ErrorCode,
            ErrorMessage = mapped.ErrorMessage,
            FieldErrors  = mapped.FieldErrors,
            TraceId      = traceId
        };

        return new ObjectResult(response) { StatusCode = statusCode };
    }

    internal static string? ResolveTraceId()
        => Activity.Current?.TraceId.ToHexString() is { Length: > 0 } id ? id : null;


    public static IResult ToHttpResult<T>(this Result<T> result, IHttpErrorMapper mapper)
        where T : notnull
    {
        if (result.IsSuccess)
            return Results.Ok(new HttpResponse<T> { Success = true, Data = result.Value });

        var first = result.Errors.FirstOrDefault();
        if (first is null)
            return Results.Problem(statusCode: 500);

        var statusCode = mapper.MapToStatusCode(first.Type);
        var response = mapper.Map(first);

        return statusCode switch
        {
            400 => Results.BadRequest(response),
            401 => Results.Unauthorized(),
            403 => Results.Forbid(),
            404 => Results.NotFound(response),
            409 => Results.Conflict(response),
            422 => Results.UnprocessableEntity(response),
            _   => Results.Problem(first.Message, statusCode: statusCode)
        };
    }

    public static IResult ToHttpResult(this Result result, IHttpErrorMapper mapper)
    {
        if (result.IsSuccess)
            return Results.Ok(new HttpResponse<object?> { Success = true, Data = null });

        var first = result.Errors.FirstOrDefault();
        if (first is null)
            return Results.Problem(statusCode: 500);

        var statusCode = mapper.MapToStatusCode(first.Type);
        var response = mapper.Map(first);

        return statusCode switch
        {
            400 => Results.BadRequest(response),
            401 => Results.Unauthorized(),
            403 => Results.Forbid(),
            404 => Results.NotFound(response),
            409 => Results.Conflict(response),
            422 => Results.UnprocessableEntity(response),
            _   => Results.Problem(first.Message, statusCode: statusCode)
        };
    }
}
    /// <summary>Converts a successful or failed result to an MVC action result.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">The error mapper.</param>
    /// <returns>The MVC action result.</returns>
    /// <summary>Converts a successful or failed generic result to a Minimal API result.</summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">The error mapper.</param>
    /// <returns>The Minimal API result.</returns>
    /// <summary>Converts a successful or failed result to a Minimal API result.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">The error mapper.</param>
    /// <returns>The Minimal API result.</returns>
