using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    /// <summary>Registration helpers for request post-processors.</summary>
    public static class MediatorRequestPostProcessorsDI
    {
        /// <summary>Scans an assembly and registers every <see cref="IRequestPostProcessor{TRequest,TResponse}"/> found.</summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="lifetime">Lifetime applied to the registrations.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddRequestPostProcessorsFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            MediatorServicesDI.RegisterOpenGenericImplementations(
                services,
                assembly,
                typeof(IRequestPostProcessor<,>),
                lifetime);

            return services;
        }

        /// <summary>Registers a single request post-processor for a specific request/response pair.</summary>
        /// <typeparam name="TRequest">The request type handled.</typeparam>
        /// <typeparam name="TResponse">The response type produced.</typeparam>
        /// <typeparam name="TProcessor">The post-processor implementation.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="lifetime">Lifetime applied to the registration.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
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
