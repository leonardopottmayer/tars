using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator.DI
{
    /// <summary>Configures which assemblies <see cref="MediatorServicesDI.AddTarsMediator"/> scans for handlers.</summary>
    public sealed class MediatorOptions
    {
        private readonly List<(Assembly Assembly, ServiceLifetime Lifetime)> _handlerAssemblies = [];

        internal IReadOnlyList<(Assembly Assembly, ServiceLifetime Lifetime)> HandlerAssemblies => _handlerAssemblies;

        /// <summary>Registers a single assembly to be scanned for handlers.</summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="lifetime">Lifetime applied to the discovered registrations.</param>
        /// <returns>This <see cref="MediatorOptions"/> for chaining.</returns>
        public MediatorOptions RegisterHandlersFromAssembly(Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            _handlerAssemblies.Add((assembly, lifetime));
            return this;
        }

        /// <summary>Registers multiple assemblies to be scanned for handlers.</summary>
        /// <param name="lifetime">Lifetime applied to the discovered registrations.</param>
        /// <param name="assemblies">The assemblies to scan.</param>
        /// <returns>This <see cref="MediatorOptions"/> for chaining.</returns>
        public MediatorOptions RegisterHandlersFromAssemblies(ServiceLifetime lifetime = ServiceLifetime.Scoped, params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
                _handlerAssemblies.Add((assembly, lifetime));
            return this;
        }
    }
}
