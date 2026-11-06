using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.Security.Authorization.Abstractions;
using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.AspNetCore.Extensions;

namespace Pottmayer.Tars.Security.Authorization.AspNetCore.Filters;

/// <summary>
/// Global MVC filter: runs <see cref="AuthorizationGate"/> for the <see cref="RequiresPermissionAttribute"/>s
/// on the controller and the action, and answers a refusal through the registered
/// <see cref="IHttpErrorMapper"/>.
/// </summary>
internal sealed class PermissionActionFilter(
    AuthorizationGate gate,
    IHttpErrorMapper errorMapper,
    PermissionMvcFilterConfiguration configuration) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var permissions = context.ActionDescriptor.EndpointMetadata
            .OfType<RequiresPermissionAttribute>()
            .Select(a => a.Permission)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (permissions.Length > 0)
        {
            var subject = configuration.SubjectSelector(context.HttpContext.User);
            var error = await gate.CheckAsync(subject, permissions, context.HttpContext.RequestAborted);
            if (error is not null)
            {
                context.Result = Result.Failure(error).ToActionResult(errorMapper);
                return;
            }
        }

        await next();
    }
}

/// <summary>
/// How <see cref="PermissionActionFilter"/> finds the subject in the current principal.
/// </summary>
public sealed class PermissionMvcFilterConfiguration
{
    /// <summary>
    /// Reads the subject id from the principal. Default: the <c>sub</c> claim, then
    /// <see cref="ClaimTypes.NameIdentifier"/> (where the JWT handler maps <c>sub</c> by default).
    /// </summary>
    public Func<ClaimsPrincipal, string?> SubjectSelector { get; set; } = principal =>
        principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
