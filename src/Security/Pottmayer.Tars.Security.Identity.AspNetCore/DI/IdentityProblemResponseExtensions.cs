using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.Web.Http;
using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.DI;

/// <summary>
/// Wires JwtBearer's challenge (401) and forbidden (403) events so that, instead of an empty
/// body, they emit the same <see cref="HttpErrorResponse"/> envelope produced by the registered
/// <see cref="IHttpErrorMapper"/> — keeping middleware-level auth responses consistent with the
/// rest of the API.
/// </summary>
public static class IdentityProblemResponseExtensions
{
    public static JwtBearerOptions ConfigureTarsIdentityProblemResponses(
        this JwtBearerOptions options,
        Error? unauthorizedError = null,
        Error? forbiddenError = null)
    {
        var unauthorized = unauthorizedError ?? Error.Unauthorized("UNAUTHORIZED", string.Empty);
        var forbidden    = forbiddenError    ?? Error.Forbidden("FORBIDDEN", string.Empty);

        options.Events ??= new JwtBearerEvents();

        options.Events.OnChallenge = async context =>
        {
            context.HandleResponse();
            await WriteProblemAsync(context.HttpContext, StatusCodes.Status401Unauthorized, unauthorized);
        };

        options.Events.OnForbidden = context
            => WriteProblemAsync(context.HttpContext, StatusCodes.Status403Forbidden, forbidden);

        return options;
    }

    private static async Task WriteProblemAsync(HttpContext httpContext, int statusCode, Error error)
    {
        if (httpContext.Response.HasStarted)
            return;

        var mapper = httpContext.RequestServices.GetRequiredService<IHttpErrorMapper>();
        var mapped = mapper.Map(error);

        var body = new HttpErrorResponse
        {
            Success      = false,
            ErrorCode    = mapped.ErrorCode,
            ErrorMessage = mapped.ErrorMessage,
            FieldErrors  = mapped.FieldErrors,
            TraceId      = Activity.Current?.TraceId.ToHexString()
        };

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(body);
    }
}
