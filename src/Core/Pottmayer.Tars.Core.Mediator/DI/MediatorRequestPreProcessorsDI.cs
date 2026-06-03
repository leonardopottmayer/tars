using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    public static class MediatorRequestPreProcessorsDI
    {
        public static IServiceCollection AddRequestPreProcessorsFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            MediatorServicesDI.RegisterOpenGenericImplementations(
                services,
                assembly,
                typeof(IRequestPreProcessor<>),
                lifetime,
                isOpenGenericInterface: true);

            return services;
        }

        public static IServiceCollection AddRequestPreProcessor<TRequest, TProcessor>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TRequest : IRequest
            where TProcessor : class, IRequestPreProcessor<TRequest>
        {
            services.Add(new ServiceDescriptor(typeof(IRequestPreProcessor<TRequest>), typeof(TProcessor), lifetime));
            return services;
        }
    }
}
