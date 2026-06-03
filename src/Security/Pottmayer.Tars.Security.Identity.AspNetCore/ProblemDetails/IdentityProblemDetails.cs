using Microsoft.AspNetCore.Http;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.ProblemDetails;

public static class IdentityProblemDetails
{
    public static Microsoft.AspNetCore.Mvc.ProblemDetails Validation(string title, string detail, string? instance = null)
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status400BadRequest,
            Instance = instance
        };
    }

    public static Microsoft.AspNetCore.Mvc.ProblemDetails Unauthorized(string title = "Unauthorized", string detail = "Authentication failed.", string? instance = null)
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status401Unauthorized,
            Instance = instance
        };
    }
}
