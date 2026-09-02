using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Mediator.Abstractions;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    /// <summary>Registration helpers for the mediator and its handlers, behaviors and processors.</summary>
    public static class MediatorServicesDI
    {
        /// <summary>
        /// Registers the mediator (<see cref="IMediator"/>, <see cref="ISender"/>, <see cref="IPublisher"/>)
        /// and, for each assembly configured via <paramref name="configure"/>, its handlers, behaviors and processors.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="configure">Optional configuration selecting handler assemblies and lifetimes.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddTarsMediator(
            this IServiceCollection services,
            Action<MediatorOptions>? configure = null)
        {
            services.TryAddScoped<IMediator, Mediator>();
            services.TryAddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
            services.TryAddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

            var options = new MediatorOptions();
            configure?.Invoke(options);

            foreach (var (assembly, lifetime) in options.HandlerAssemblies)
                services.AddMediatorHandlersFromAssemblies([assembly], lifetime);

            return services;
        }

        /// <summary>
        /// Scans the given assemblies (or the calling assembly when none are supplied) and registers every
        /// request handler, notification handler, pipeline behavior, and request pre-/post-processor found.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="assemblies">Assemblies to scan; defaults to the calling assembly when null or empty.</param>
        /// <param name="lifetime">Lifetime applied to the discovered registrations.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddMediatorHandlersFromAssemblies(
            this IServiceCollection services,
            Assembly[]? assemblies = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            var asms = assemblies is null || assemblies.Length == 0
                ? [Assembly.GetCallingAssembly()]
                : assemblies;

            foreach (var assembly in asms)
            {
                services.AddRequestHandlersFromAssembly(assembly, lifetime);
                services.AddNotificationHandlersFromAssembly(assembly, lifetime);
                services.AddPipelineBehaviorsFromAssembly(assembly, lifetime);
                services.AddRequestPreProcessorsFromAssembly(assembly, lifetime);
                services.AddRequestPostProcessorsFromAssembly(assembly, lifetime);
            }

            return services;
        }

        /// <summary>
        /// Scans <paramref name="assembly"/> for concrete classes implementing the given open-generic
        /// interface and registers each match, using the generic type definition for open-generic implementations
        /// so the container can close it on resolution.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="openGenericInterfaceType">The open-generic interface definition to match (e.g. <c>IRequestHandler&lt;,&gt;</c>).</param>
        /// <param name="lifetime">Lifetime applied to the registrations.</param>
        /// <exception cref="ArgumentException"><paramref name="openGenericInterfaceType"/> is not a generic type definition.</exception>
        public static void RegisterOpenGenericImplementations(
            IServiceCollection services,
            Assembly assembly,
            Type openGenericInterfaceType,
            ServiceLifetime lifetime)
        {
            if (!openGenericInterfaceType.IsGenericTypeDefinition)
                throw new ArgumentException("Interface type must be an open generic type definition.", nameof(openGenericInterfaceType));

            var interfaceTypeArgs = openGenericInterfaceType.GetGenericArguments().Length;
            var types = assembly.GetExportedTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false });

            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterfaceType);

                foreach (var interfaceType in interfaces)
                {
                    if (interfaceType.GetGenericArguments().Length != interfaceTypeArgs)
                        continue;

                    // For open generic types (e.g. RequestLoggingBehavior<,>), register using the generic type definition
                    // so the container can close it when resolving (e.g. IPipelineBehavior<,> -> RequestLoggingBehavior<,>)
                    var serviceType = type.IsGenericTypeDefinition ? openGenericInterfaceType : interfaceType;
                    services.Add(new ServiceDescriptor(serviceType, type, lifetime));
                }
            }
        }
    }
}
