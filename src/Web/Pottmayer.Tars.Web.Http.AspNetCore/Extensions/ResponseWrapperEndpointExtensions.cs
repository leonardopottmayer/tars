using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pottmayer.Tars.Web.Http.AspNetCore.Filters;
using Pottmayer.Tars.Web.Http.AspNetCore.Metadata;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Extensions;

public static class ResponseWrapperEndpointExtensions
{
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
