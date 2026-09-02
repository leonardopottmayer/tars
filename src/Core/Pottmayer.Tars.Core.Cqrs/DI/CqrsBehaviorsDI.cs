using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Cqrs.Behaviors;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.DI
{
    /// <summary>Registration helpers for the CQRS exception-mapping pipeline behavior.</summary>
    public static class CqrsBehaviorsDI
    {
        /// <summary>
        /// Registers <see cref="Behaviors.ExceptionMappingBehavior{TRequest,TResponse}"/> as a scoped pipeline
        /// behavior, optionally wiring a custom exception-to-errors mapper.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="customMapper">Optional mapper for exceptions that don't implement <see cref="IExpectedException"/>.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddTarsCqrsExceptionMappingBehavior(
            this IServiceCollection services,
            Func<Exception, IReadOnlyList<Error>>? customMapper = null)
        {
            if (customMapper is not null)
                services.AddTarsCqrsExceptionMappingConfiguration(customMapper);

            services.TryAddScoped(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));

            return services;
        }

        /// <summary>
        /// Registers a singleton <see cref="ExceptionMappingConfiguration"/> holding an optional custom
        /// exception-to-errors mapper used by the exception-mapping behavior.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="customMapper">Optional mapper for exceptions that don't implement <see cref="IExpectedException"/>.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddTarsCqrsExceptionMappingConfiguration(
            this IServiceCollection services,
            Func<Exception, IReadOnlyList<Error>>? customMapper = null)
        {
            services.TryAddSingleton(new ExceptionMappingConfiguration
            {
                CustomMapper = customMapper
            });

            return services;
        }
    }
}
