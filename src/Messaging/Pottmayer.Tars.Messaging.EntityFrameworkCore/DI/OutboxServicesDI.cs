using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Dispatch;
using Pottmayer.Tars.Messaging.Broker.Registry;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Options;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Relay;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.DI;

/// <summary>
/// The pieces of the transactional in-process outbox, registered one at a time. There is deliberately
/// no "register everything" method — the application composes exactly what it uses, the same way a
/// broker provider composes the shared broker runtime.
/// </summary>
/// <remarks>
/// <para>A complete setup at the composition root looks like this:</para>
/// <code>
/// using Pottmayer.Tars.Messaging.Broker.DI;              // registry + last-mile dispatcher (reused)
/// using Pottmayer.Tars.Messaging.EntityFrameworkCore.DI; // the outbox pieces
///
/// // 1. Resolve a stored event name back to its type — scan every contracts assembly.
/// services.AddTarsIntegrationEventTypeRegistry(
///     typeof(AccountActivationRequested).Assembly,
///     typeof(NotifyUserRequested).Assembly);
///
/// // 2. The last mile: hand a delivered event to its IIntegrationEventHandler&lt;T&gt; implementations.
/// services.AddTarsIntegrationEventDispatcher();
///
/// // 3. Serialize the event body/headers to and from the outbox row.
/// services.AddTarsIntegrationEventSerializer();
///
/// // 4. Make PublishAsync write an outbox row in the producer's transaction (replaces the in-process bus).
/// services.AddTarsOutboxBus();
///
/// // 5. The outbox table's repository.
/// services.AddTarsOutboxStore();
///
/// // 6. Optional: let aggregates raise domain events that translators turn into integration events at commit.
/// services.AddTarsOutboxDomainEventDispatcher();
///
/// // 7. One relay per producing database (each hosts its own outbox table).
/// services.AddTarsOutboxRelay("identity");
/// services.AddTarsOutboxRelay("agenda", o =&gt; o.PollingInterval = TimeSpan.FromSeconds(2));
///
/// // 8. Register consumers (and, if you use step 6, the domain-event translators), once per module.
/// services.AddTarsIntegrationEventHandlers(typeof(SomeConsumer).Assembly); // from Broker.DI
/// services.AddTarsDomainEventHandlers(typeof(SomeTranslator).Assembly);
/// </code>
/// <para>
/// Steps 1, 2 and 8's <c>AddTarsIntegrationEventHandlers</c> come from the broker runtime
/// (<c>Pottmayer.Tars.Messaging.Broker.DI</c>) and are reused as-is; the rest live here. Everything is
/// idempotent (<c>TryAdd</c>) except the bus and the relays, so ordering does not matter.
/// </para>
/// </remarks>
public static class OutboxServicesDI
{
    /// <summary>
    /// Registers <see cref="JsonIntegrationEventSerializer"/> as the outbox serializer. Registering a
    /// replacement first (or via <see cref="AddTarsIntegrationEventSerializer{TSerializer}"/>) wins.
    /// </summary>
    public static IServiceCollection AddTarsIntegrationEventSerializer(this IServiceCollection services)
        => services.AddTarsIntegrationEventSerializer<JsonIntegrationEventSerializer>();

    /// <summary>Registers a custom <see cref="IIntegrationEventSerializer"/> — e.g. to match the wire format of a broker you will later adopt.</summary>
    public static IServiceCollection AddTarsIntegrationEventSerializer<TSerializer>(this IServiceCollection services)
        where TSerializer : class, IIntegrationEventSerializer
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IIntegrationEventSerializer, TSerializer>();
        return services;
    }

    /// <summary>
    /// Registers the outbox-backed <see cref="IIntegrationEventBus"/>, so <c>PublishAsync</c> writes an
    /// <see cref="OutboxMessage"/> in the producer's ambient transaction instead of delivering inline.
    /// </summary>
    /// <remarks>
    /// Any previously registered <see cref="IIntegrationEventBus"/> (e.g. the in-process bus) is removed
    /// first, so this one wins unconditionally — a stale in-process bus would silently reopen the
    /// dual-write gap this closes.
    /// </remarks>
    public static IServiceCollection AddTarsOutboxBus(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.RemoveAll<IIntegrationEventBus>();
        services.AddSingleton<IIntegrationEventBus, OutboxIntegrationEventBus>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IOutboxRepository"/>, the store the bus writes to and the relay drains.
    /// Transient, like other Tars repositories: each instance captures the ambient context at construction.
    /// </summary>
    public static IServiceCollection AddTarsOutboxStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddTransient<IOutboxRepository, OutboxRepository>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="OutboxDomainEventDispatcher"/> as the <see cref="IDomainEventDispatcher"/> the
    /// data layer runs at commit, so aggregates that <c>Raise</c> domain events can have translators turn
    /// them into integration events written to the same transaction. Optional — only needed if you use
    /// domain events to produce integration events.
    /// </summary>
    public static IServiceCollection AddTarsOutboxDomainEventDispatcher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<IDomainEventDispatcher, OutboxDomainEventDispatcher>();
        return services;
    }

    /// <summary>
    /// Switches every relay from local dispatch to <b>forwarding to a broker</b>: drained events are
    /// published through the transport bus registered under <paramref name="transportKey"/> (a composite
    /// route key, or an <c>AddTarsKeyed{RabbitMq,Kafka}IntegrationEventBus</c> key). This is what makes
    /// the in-process outbox a transactional outbox <em>to Kafka or RabbitMQ</em> — the row is still
    /// written in the producer's transaction; the relay just delivers it to the broker instead of to
    /// local handlers.
    /// </summary>
    /// <remarks>
    /// Without this call the relay delivers locally (the modular-monolith default). Register the keyed
    /// transport before or after — the relay resolves the delivery lazily when it starts. The delivery is
    /// process-wide: all relays share it. (An alternative to running the relay at all is to leave the
    /// table for an external CDC reader such as Debezium — see the outbox documentation.)
    /// </remarks>
    public static IServiceCollection AddTarsOutboxBrokerDelivery(this IServiceCollection services, string transportKey)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportKey);

        services.RemoveAll<IOutboxRelayDelivery>();
        services.AddSingleton<IOutboxRelayDelivery>(
            sp => new BrokerOutboxDelivery(sp.GetRequiredService<IServiceScopeFactory>(), transportKey));
        return services;
    }

    /// <summary>
    /// Registers one background relay for <paramref name="databaseKey"/>: it drains that database's outbox,
    /// delivering due messages to the local handlers with retry, backoff and dead-lettering, and purging
    /// dispatched rows. Call once per producing database.
    /// </summary>
    /// <remarks>
    /// The relay resolves the registry, last-mile dispatcher and serializer at runtime, so those must be
    /// registered too (steps 1–3 above) — but in any order relative to this call.
    /// </remarks>
    public static IServiceCollection AddTarsOutboxRelay(
        this IServiceCollection services,
        string databaseKey,
        Action<OutboxDatabaseOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IHostedService>(sp =>
        {
            // Seed from the config-bound fleet defaults (or the built-ins when AddTarsOutboxOptions was
            // not called), then apply the per-database override.
            var defaults = sp.GetService<IOptions<OutboxOptions>>()?.Value;
            var options = new OutboxDatabaseOptions(databaseKey, defaults);
            configure?.Invoke(options);

            if (!options.IsValid())
            {
                throw new InvalidOperationException($"Invalid OutboxDatabaseOptions for database '{databaseKey}'.");
            }

            return new OutboxRelayService(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IIntegrationEventTypeRegistry>(),
                // Local dispatch is the default; AddTarsOutboxBrokerDelivery swaps in broker forwarding.
                sp.GetService<IOutboxRelayDelivery>()
                    ?? new LocalHandlerOutboxDelivery(sp.GetRequiredService<IIntegrationEventDispatcher>()),
                sp.GetRequiredService<IIntegrationEventSerializer>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<OutboxRelayService>>(),
                options);
        });

        return services;
    }

    /// <summary>
    /// Scans an assembly and registers every <see cref="IDomainEventHandler{T}"/> — the translators that
    /// turn domain events into integration events at commit time. Call once per module that raises domain
    /// events. (Integration event <em>consumers</em> are registered with the broker runtime's
    /// <c>AddTarsIntegrationEventHandlers</c>.)
    /// </summary>
    public static IServiceCollection AddTarsDomainEventHandlers(
        this IServiceCollection services, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var handlerInterface = typeof(IDomainEventHandler<>);

        foreach (var type in assembly.GetExportedTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            var closedInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface);

            foreach (var closedInterface in closedInterfaces)
                services.TryAddEnumerable(new ServiceDescriptor(closedInterface, type, lifetime));
        }

        return services;
    }
}
