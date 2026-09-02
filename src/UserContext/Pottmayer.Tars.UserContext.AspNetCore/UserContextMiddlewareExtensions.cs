using Microsoft.AspNetCore.Builder;

namespace Pottmayer.Tars.UserContext.AspNetCore;

/// <summary>
/// Extension methods for registering <see cref="UserContextMiddleware"/> in the ASP.NET Core pipeline.
/// </summary>
public static class UserContextMiddlewareExtensions
{
    /// <summary>
    /// Adds <see cref="UserContextMiddleware"/> to the pipeline.
    /// Must be called after authentication middleware so that <c>HttpContext.User</c> is already populated.
    /// </summary>
    public static IApplicationBuilder UseTarsUserContext(this IApplicationBuilder app)
        => app.UseMiddleware<UserContextMiddleware>();
}
