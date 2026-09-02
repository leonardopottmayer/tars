using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    /// <summary>Registration helpers for pipeline behaviors.</summary>
    public static class MediatorPipelineBehaviorsDI
    {
        /// <summary>Scans an assembly and registers every <see cref="IPipelineBehavior{TRequest,TResponse}"/> found.</summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="lifetime">Lifetime applied to the registrations.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddPipelineBehaviorsFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            MediatorServicesDI.RegisterOpenGenericImplementations(
                services,
                assembly,
                typeof(IPipelineBehavior<,>),
                lifetime);

            return services;
        }

        /// <summary>
        /// Registers a single pipeline behavior, wiring it to every <see cref="IPipelineBehavior{TRequest,TResponse}"/>
        /// it implements.
        /// </summary>
        /// <typeparam name="TBehavior">The behavior implementation.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="lifetime">Lifetime applied to the registration.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddPipelineBehavior<TBehavior>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TBehavior : class
        {
            var behaviorInterfaces = typeof(TBehavior).GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

            foreach (var behaviorInterface in behaviorInterfaces)
                services.Add(new ServiceDescriptor(behaviorInterface, typeof(TBehavior), lifetime));

            return services;
        }
    }
}
