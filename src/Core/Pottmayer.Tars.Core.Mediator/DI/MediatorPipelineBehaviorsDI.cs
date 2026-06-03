using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    public static class MediatorPipelineBehaviorsDI
    {
        public static IServiceCollection AddPipelineBehaviorsFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            MediatorServicesDI.RegisterOpenGenericImplementations(
                services,
                assembly,
                typeof(IPipelineBehavior<,>),
                lifetime,
                isOpenGenericInterface: true);

            return services;
        }

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
