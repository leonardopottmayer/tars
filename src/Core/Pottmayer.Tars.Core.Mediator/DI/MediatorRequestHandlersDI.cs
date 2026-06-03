using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    public static class MediatorRequestHandlersDI
    {
        public static IServiceCollection AddRequestHandlersFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            MediatorServicesDI.RegisterOpenGenericImplementations(
                services,
                assembly,
                typeof(IRequestHandler<,>),
                lifetime,
                isOpenGenericInterface: true);

            return services;
        }

        public static IServiceCollection AddRequestHandler<TRequest, TResponse, THandler>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TRequest : IRequest<TResponse>
            where THandler : class, IRequestHandler<TRequest, TResponse>
        {
            services.Add(new ServiceDescriptor(typeof(IRequestHandler<TRequest, TResponse>), typeof(THandler), lifetime));
            return services;
        }
    }
}
