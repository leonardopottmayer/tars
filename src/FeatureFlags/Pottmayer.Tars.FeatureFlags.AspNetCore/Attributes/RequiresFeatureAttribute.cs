using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.FeatureFlags.Abstractions;
using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.AspNetCore.Extensions;

namespace Pottmayer.Tars.FeatureFlags.AspNetCore.Attributes;

/// <summary>
/// Short-circuits a controller or action with a <see cref="ErrorType.NotFound"/> error
/// (<see cref="FeatureFlagErrors.Disabled"/>) while the flag is off. Several attributes must all be on.
/// </summary>
/// <remarks>
/// Needs <see cref="IFeatureFlags"/> and an <see cref="IHttpErrorMapper"/> in DI. The error goes
/// through the mapper, so the response has the same envelope as any other Tars error.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiresFeatureAttribute(string key) : Attribute, IAsyncActionFilter
{
    /// <summary>The flag that must be on.</summary>
    public string Key { get; } = key;

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var flags = services.GetRequiredService<IFeatureFlags>();

        if (await flags.IsEnabledAsync(Key, context.HttpContext.RequestAborted))
        {
            await next();
            return;
        }

        var error = Error.NotFound(FeatureFlagErrors.Disabled, $"Feature '{Key}' is disabled.");
        context.Result = Result.Failure(error).ToActionResult(services.GetRequiredService<IHttpErrorMapper>());
    }
}
