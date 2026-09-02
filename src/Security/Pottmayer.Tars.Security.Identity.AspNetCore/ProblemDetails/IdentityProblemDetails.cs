using Microsoft.AspNetCore.Http;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.ProblemDetails;

/// <summary>
/// Builds RFC 7807 <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> for common Identity error responses.
/// </summary>
public static class IdentityProblemDetails
{
    /// <summary>Builds a 400 Bad Request problem details for a validation failure.</summary>
    /// <param name="title">The problem title.</param>
    /// <param name="detail">The problem detail message.</param>
    /// <param name="instance">Optional URI identifying the specific occurrence.</param>
    /// <returns>The problem details.</returns>
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

    /// <summary>Builds a 401 Unauthorized problem details for an authentication failure.</summary>
    /// <param name="title">The problem title.</param>
    /// <param name="detail">The problem detail message.</param>
    /// <param name="instance">Optional URI identifying the specific occurrence.</param>
    /// <returns>The problem details.</returns>
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
