using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    public static class MediatorRequestPostProcessorsDI
    {
        public static IServiceCollection AddRequestPostProcessorsFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            MediatorServicesDI.RegisterOpenGenericImplementations(
                services,
                assembly,
                typeof(IRequestPostProcessor<,>),
                lifetime,
                isOpenGenericInterface: true);

            return services;
        }

        public static IServiceCollection AddRequestPostProcessor<TRequest, TResponse, TProcessor>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TRequest : IRequest<TResponse>
            where TProcessor : class, IRequestPostProcessor<TRequest, TResponse>
        {
            services.Add(new ServiceDescriptor(typeof(IRequestPostProcessor<TRequest, TResponse>), typeof(TProcessor), lifetime));
            return services;
        }
    }
}
