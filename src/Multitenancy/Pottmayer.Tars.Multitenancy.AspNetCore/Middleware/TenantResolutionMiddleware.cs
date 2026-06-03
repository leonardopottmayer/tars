using Microsoft.AspNetCore.Http;
using Pottmayer.Tars.Multitenancy.Abstractions.Context;
using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;
using Pottmayer.Tars.Multitenancy.AspNetCore.Resolvers;

namespace Pottmayer.Tars.Multitenancy.AspNetCore.Middleware;

/// <summary>
/// ASP.NET Core middleware that runs the tenant resolver pipeline on each request
/// and sets <see cref="ITenantContextAccessor.Current"/> for downstream handlers.
/// Register with <c>app.UseTarsTenantResolution()</c>.
/// </summary>
public sealed class TarsTenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TarsTenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantResolverPipeline pipeline,
        ITenantContextFactory contextFactory,
        ITenantContextAccessor accessor)
    {
        var httpData = new TenantHttpRequestData(
            context.Request.Host.Host,
            context.Request.Headers.ToDictionary(
                h => h.Key,
                h => (string?)h.Value.ToString(),
                StringComparer.OrdinalIgnoreCase));

        var items = new Dictionary<string, object?>
        {
            [TenantResolutionHttpKeys.HttpRequestData] = httpData
        };

        var request = new TenantResolutionRequest
        {
            Services = context.RequestServices,
            Principal = context.User,
            Items = items
        };

        var result = await pipeline.ResolveAsync(request, context.RequestAborted).ConfigureAwait(false);
        accessor.SetCurrent(contextFactory.Create(result));

        await _next(context).ConfigureAwait(false);
    }
}
