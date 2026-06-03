using Microsoft.AspNetCore.Http;
using Pottmayer.Tars.UserContext.Abstractions;

namespace Pottmayer.Tars.UserContext.AspNetCore;

/// <summary>
/// Sets <see cref="IUserContextAccessor.Current"/> from the authenticated HTTP principal
/// before the request reaches downstream middleware and handlers.
/// Clears the context on the way out to avoid AsyncLocal value leakage in thread pool scenarios.
/// </summary>
public sealed class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context, IUserContextAccessor accessor)
    {
        if (context.User.Identity?.IsAuthenticated == true)
            accessor.Current = new Tars.UserContext.UserContext(context.User.Claims.ToList());

        try
        {
            await _next(context);
        }
        finally
        {
            accessor.Current = null;
        }
    }
}
