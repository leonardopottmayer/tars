using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    /// <summary>Registration helpers for request handlers.</summary>
    public static class MediatorRequestHandlersDI
    {
        /// <summary>Scans an assembly and registers every <see cref="IRequestHandler{TRequest,TResponse}"/> found.</summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="lifetime">Lifetime applied to the registrations.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddRequestHandlersFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            MediatorServicesDI.RegisterOpenGenericImplementations(
                services,
                assembly,
                typeof(IRequestHandler<,>),
                lifetime);

            return services;
        }

        /// <summary>Registers a single request handler for a specific request/response pair.</summary>
        /// <typeparam name="TRequest">The request type handled.</typeparam>
        /// <typeparam name="TResponse">The response type produced.</typeparam>
        /// <typeparam name="THandler">The handler implementation.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="lifetime">Lifetime applied to the registration.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddRequestHandler<TRequest, TResponse, THandler>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TRequest : IRequest<TResponse>
            where THandler : class, IRequestHandler<TRequest, TResponse>
        {
            services.Add(new ServiceDescriptor(typeof(IRequestHandler<TRequest, TResponse>), typeof(THandler), lifetime));
            return services;
        }
    }
}
