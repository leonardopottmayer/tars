using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    /// <summary>Registration helpers for request pre-processors.</summary>
    public static class MediatorRequestPreProcessorsDI
    {
        /// <summary>Scans an assembly and registers every <see cref="IRequestPreProcessor{TRequest}"/> found.</summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="lifetime">Lifetime applied to the registrations.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddRequestPreProcessorsFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            MediatorServicesDI.RegisterOpenGenericImplementations(
                services,
                assembly,
                typeof(IRequestPreProcessor<>),
                lifetime);

            return services;
        }

        /// <summary>Registers a single request pre-processor for a specific request type.</summary>
        /// <typeparam name="TRequest">The request type handled.</typeparam>
        /// <typeparam name="TProcessor">The pre-processor implementation.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="lifetime">Lifetime applied to the registration.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
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
