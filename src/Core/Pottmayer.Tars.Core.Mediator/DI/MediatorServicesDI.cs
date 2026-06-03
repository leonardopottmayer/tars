using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Mediator.Abstractions;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    public static class MediatorServicesDI
    {
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

        public static void RegisterOpenGenericImplementations(
            IServiceCollection services,
            Assembly assembly,
            Type openGenericInterfaceType,
            ServiceLifetime lifetime,
            bool isOpenGenericInterface)
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
