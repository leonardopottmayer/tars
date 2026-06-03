using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Extensions;

public static class ResultHttpExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, IHttpErrorMapper mapper)
        where T : notnull
    {
        if (result.IsSuccess)
            return new OkObjectResult(new HttpResponse<T> { Success = true, Data = result.Value });

        var first = result.Errors.FirstOrDefault();
        if (first is null)
            return new StatusCodeResult(500);

        var statusCode = mapper.MapToStatusCode(first.Type);
        var response   = mapper.Map(first);

        return statusCode switch
        {
            400 => new BadRequestObjectResult(response),
            401 => new UnauthorizedResult(),
            403 => new ForbidResult(),
            404 => new NotFoundObjectResult(response),
            409 => new ConflictObjectResult(response),
            422 => new UnprocessableEntityObjectResult(response),
            _   => new ObjectResult(first.Message) { StatusCode = statusCode }
        };
    }

    public static IActionResult ToActionResult(this Result result, IHttpErrorMapper mapper)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new HttpResponse<object?> { Success = true, Data = null });

        var first = result.Errors.FirstOrDefault();
        if (first is null)
            return new StatusCodeResult(500);

        var statusCode = mapper.MapToStatusCode(first.Type);
        var response   = mapper.Map(first);

        return statusCode switch
        {
            400 => new BadRequestObjectResult(response),
            401 => new UnauthorizedResult(),
            403 => new ForbidResult(),
            404 => new NotFoundObjectResult(response),
            409 => new ConflictObjectResult(response),
            422 => new UnprocessableEntityObjectResult(response),
            _   => new ObjectResult(first.Message) { StatusCode = statusCode }
        };
    }


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
