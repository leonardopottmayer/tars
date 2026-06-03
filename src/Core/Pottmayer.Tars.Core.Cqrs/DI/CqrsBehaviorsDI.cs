using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Cqrs.Behaviors;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.DI
{
    public static class CqrsBehaviorsDI
    {
        public static IServiceCollection AddTarsCqrsExceptionMappingBehavior(
            this IServiceCollection services,
            Func<Exception, IReadOnlyList<Error>>? customMapper = null)
        {
            if (customMapper is not null)
                services.AddTarsCqrsExceptionMappingConfiguration(customMapper);

            services.TryAddScoped(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));

            return services;
        }

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
