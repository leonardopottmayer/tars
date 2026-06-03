using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Notifications;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    public static class MediatorNotificationHandlersDI
    {
        public static IServiceCollection AddNotificationHandlersFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            MediatorServicesDI.RegisterOpenGenericImplementations(
                services,
                assembly,
                typeof(INotificationHandler<>),
                lifetime,
                isOpenGenericInterface: true);

            return services;
        }

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
