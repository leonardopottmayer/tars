using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pottmayer.Tars.Web.Http.AspNetCore.Filters;
using Pottmayer.Tars.Web.Http.AspNetCore.Metadata;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Extensions;

/// <summary>
/// Provides Minimal API response-wrapping conventions.
/// </summary>
public static class ResponseWrapperEndpointExtensions
{
    /// <summary>Enables response wrapping for a route group.</summary>
    /// <param name="builder">The route group builder.</param>
    /// <returns>The configured route group builder.</returns>
    public static RouteGroupBuilder AddTarsResponseWrapper(this RouteGroupBuilder builder)
        => builder
            .WithMetadata(new ResponseWrapperMetadata())
            .AddEndpointFilter<ResponseWrapperEndpointFilter>();

    public static RouteHandlerBuilder AddTarsResponseWrapper(this RouteHandlerBuilder builder)
        => builder
            .WithMetadata(new ResponseWrapperMetadata())
            .AddEndpointFilter<ResponseWrapperEndpointFilter>();

    public static T DisableTarsResponseWrapper<T>(this T builder) where T : IEndpointConventionBuilder
    {
        builder.WithMetadata(new DisableResponseWrapperMetadata());
        return builder;
    }
}
    /// <summary>Enables response wrapping for a route handler.</summary>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The configured route handler builder.</returns>
    /// <summary>Disables response wrapping for an endpoint convention builder.</summary>
    /// <typeparam name="T">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The configured endpoint convention builder.</returns>
