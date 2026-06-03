using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    public sealed class MediatorOptions
    {
        private readonly List<(Assembly Assembly, ServiceLifetime Lifetime)> _handlerAssemblies = [];

        internal IReadOnlyList<(Assembly Assembly, ServiceLifetime Lifetime)> HandlerAssemblies => _handlerAssemblies;

        public MediatorOptions RegisterHandlersFromAssembly(Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            _handlerAssemblies.Add((assembly, lifetime));
            return this;
        }

        public MediatorOptions RegisterHandlersFromAssemblies(ServiceLifetime lifetime = ServiceLifetime.Scoped, params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
                _handlerAssemblies.Add((assembly, lifetime));
            return this;
        }
    }
}
