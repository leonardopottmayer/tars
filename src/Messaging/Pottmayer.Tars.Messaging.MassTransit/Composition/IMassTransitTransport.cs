using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.Composition;

/// <summary>
/// One transport participating in a composite (multi-transport) MassTransit application. A transport
/// carries the knowledge that is specific to its technology — how to validate itself, how to register its
/// keyed <see cref="IIntegrationEventBus"/>, how to wire its MassTransit bus — so the composite
/// orchestrator stays transport-agnostic and a new broker is added by contributing an implementation, not
/// by editing the orchestrator.
/// </summary>
/// <remarks>
/// A transport is either an <see cref="IMassTransitBusTransport"/> (a full transport that can host a bus —
/// RabbitMQ, Amazon SQS, Azure Service Bus) or an <see cref="IMassTransitRiderTransport"/> (a rider that
/// attaches to whichever bus is primary — Kafka, Event Hub). The orchestrator composes whatever set of
/// these it is given.
/// </remarks>
public interface IMassTransitTransport
{
    /// <summary>The transport key routes refer to and the composite bus resolves this transport under.</summary>
    string Key { get; }

    /// <summary>A human name for the transport technology (e.g. "RabbitMQ", "Kafka"), used in diagnostics.</summary>
    string TransportName { get; }

    /// <summary>The events, subscriptions and handler assemblies this transport declares.</summary>
    BrokerMessagingOptions Messaging { get; }

    /// <summary>
    /// Validates the transport's own configuration at startup (its options and its broker capability
    /// matrix), throwing so a misconfiguration stops the host rather than surfacing on first publish.
    /// </summary>
    void Validate();

    /// <summary>
    /// Registers this transport's keyed <see cref="IIntegrationEventBus"/> — the bus the composite resolves
    /// under <see cref="Key"/>. It must not claim the default bus, which stays the composite.
    /// </summary>
    void RegisterKeyedBus(IServiceCollection services);
}

/// <summary>
/// A full transport that can host a MassTransit bus: either the one <em>primary</em> bus of the process,
/// or its own <em>multibus</em> bus identified by a marker interface.
/// </summary>
public interface IMassTransitBusTransport : IMassTransitTransport
{
    /// <summary>
    /// Whether this transport is a candidate for the single primary bus (it was added without a marker
    /// interface). When <c>false</c>, it is a multibus bus and registers itself through
    /// <see cref="RegisterMultibus"/>.
    /// </summary>
    bool IsPrimaryCandidate { get; }

    /// <summary>
    /// Configures the shared <c>AddMassTransit</c> registration as the primary bus — consumers and
    /// registration hooks — before riders are attached. Called only when this is the chosen primary.
    /// </summary>
    void ConfigurePrimary(IBusRegistrationConfigurator bus);

    /// <summary>
    /// Selects the primary bus transport (the terminal <c>UsingRabbitMq</c>/<c>UsingAmazonSqs</c>…), after
    /// riders are attached. Called only when this is the chosen primary.
    /// </summary>
    void UsePrimaryBus(IBusRegistrationConfigurator bus);

    /// <summary>
    /// Registers this transport as its own MassTransit multibus bus (<c>AddMassTransit&lt;TBus&gt;</c>).
    /// Called only when <see cref="IsPrimaryCandidate"/> is <c>false</c>.
    /// </summary>
    void RegisterMultibus(IServiceCollection services);
}

/// <summary>
/// A rider transport that attaches to whichever bus is primary (it has no standalone bus of its own —
/// Kafka, Event Hub).
/// </summary>
public interface IMassTransitRiderTransport : IMassTransitTransport
{
    /// <summary>Attaches this rider to the shared <c>AddMassTransit</c> registration (<c>bus.AddRider</c>).</summary>
    void AttachRider(IBusRegistrationConfigurator bus);

    /// <summary>
    /// Configures the in-memory host bus the orchestrator spins up when a rider is the only transport in
    /// the process (a rider needs some bus to hang off). No-op by default.
    /// </summary>
    void ConfigureStandaloneHost(IInMemoryBusFactoryConfigurator configurator) { }
}
