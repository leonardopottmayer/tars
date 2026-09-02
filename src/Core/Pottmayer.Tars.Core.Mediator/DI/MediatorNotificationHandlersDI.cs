using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Notifications;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    /// <summary>Registration helpers for notification handlers.</summary>
    public static class MediatorNotificationHandlersDI
    {
        /// <summary>Scans an assembly and registers every <see cref="INotificationHandler{TNotification}"/> found.</summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="lifetime">Lifetime applied to the registrations.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddNotificationHandlersFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            MediatorServicesDI.RegisterOpenGenericImplementations(
                services,
                assembly,
                typeof(INotificationHandler<>),
                lifetime);

            return services;
        }

        /// <summary>Registers a single notification handler for a specific notification type.</summary>
        /// <typeparam name="TNotification">The notification type handled.</typeparam>
        /// <typeparam name="THandler">The handler implementation.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="lifetime">Lifetime applied to the registration.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddNotificationHandler<TNotification, THandler>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TNotification : INotification
            where THandler : class, INotificationHandler<TNotification>
        {
            services.Add(new ServiceDescriptor(typeof(INotificationHandler<TNotification>), typeof(THandler), lifetime));
            return services;
        }
    }
}
