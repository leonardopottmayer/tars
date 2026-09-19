# Multi Transport — several brokers behind one seam

> This builds on [brokers.md](./brokers.md). That document ends with "one broker per application"; this
> one is what you reach for when that is genuinely not enough. Read it second.

## When you actually need this

Almost always, one broker per application is the right answer, and reaching for several is complexity
you will regret. Reach for this only when you have a real, asymmetric reason — the textbook one being a
**two-plane architecture**:

- **Kafka as the event backbone** — `OrderCreated`, `PaymentProcessed`: facts, broadcast to everyone,
  replayable, high volume.
- **RabbitMQ as the work plane** — `ShipOrder`, `SendInvoice`: commands directed at a worker fleet,
  where per-message ack, retry with back-off, dead-letter queues and priority matter.

The rule that decides it is not "which broker" but **what the message is**: an *event* (something
happened) belongs on the event plane, a *command* (do this) on the work plane. If you cannot name that
asymmetry, you do not need several transports — unify on one.

A producer does **not** publish the same fact to every broker. It publishes the event once, and a
service that *reacts* drops a command onto the other transport. The composite orchestrator is what lets
that reacting service live on both planes cleanly.

## What the orchestrator is

`AddTarsMassTransitComposite` (in the provider-core package `Pottmayer.Tars.Messaging.MassTransit`) lets
one application declare **any number of transports** under keys you choose, and routes each event to one
or several of them behind the single `IIntegrationEventBus` seam. It is not fixed to a particular pair:

- **A builder, not a fixed pair** — `AddRabbitMq(key, …)` and `AddKafka(key, …)`, each under an
  arbitrary key. Declare one transport, or several; name them for their role (`"events"`, `"commands"`)
  rather than their technology.
- **One `AddMassTransit`** — one transport is the primary bus, riders (Kafka) hang off it, and every
  additional full transport is its own multibus bus.
- **The shared broker core once** — one registry, one router, one dispatcher, over the union of every
  declared transport's events and handlers.
- **A keyed bus per transport** plus **the composite bus** as the one default `IIntegrationEventBus`,
  routing by a declarative map.

### The orchestrator is transport-agnostic; the transports are packages

The orchestrator itself references **no** concrete broker. Each transport is contributed by its own
provider package through a small seam (`IMassTransitTransport`): `AddRabbitMq` lives in
`…MassTransit.RabbitMq`, `AddKafka` in `…MassTransit.Kafka`. An application **references only the
transport packages it uses** — an SQS-plus-Kafka service brings `…AmazonSqs` and `…Kafka` and never
pulls in RabbitMQ. Adding a new broker is a new provider package that contributes an
`IMassTransitBusTransport` (a full transport) or `IMassTransitRiderTransport` (a rider); the orchestrator
does not change. `A_custom_transport_contributor_composes_as_the_primary_bus` proves a transport the
orchestrator has never heard of composes purely through that seam.

## The seam is preserved

Producers keep publishing through the single `IIntegrationEventBus`:

```csharp
await bus.PublishAsync(new PaymentProcessed(/* ... */), ct);   // no idea which broker, or how many
```

The choice of transport lives in the map, decided per event *type*, not at the call site. Consumers are
unchanged too: every transport's receive side hands messages to the **same** dispatcher, so a handler
never learns which broker delivered it. Consuming from several transports is essentially free.

## Registration

```csharp
// References: Pottmayer.Tars.Messaging.MassTransit (the orchestrator) + the transport packages you use
// here, .MassTransit.Kafka and .MassTransit.RabbitMq. Nothing drags in a broker you did not name.
services.AddTarsMassTransitComposite(m =>
{
    // Declare each transport under a key you choose. Add as many as your topology needs.
    m.AddKafka("events", k =>
    {
        k.BootstrapServers = "localhost:9092";
        k.Messaging.EndpointName = "fulfillment";                       // the consumer group
        k.Messaging.RegisterEventsFromAssembly(typeof(PaymentProcessed).Assembly);
        k.Messaging.RegisterHandlersFromAssembly(typeof(PaymentProcessedHandler).Assembly);
        k.Messaging.Subscribe<PaymentProcessed>();
    });

    m.AddRabbitMq("commands", r =>
    {
        r.Host = "localhost";
        r.Username = "guest";
        r.Password = "guest";
        r.Messaging.EndpointName = "fulfillment";                       // the queue
        r.Messaging.RegisterEventsFromAssembly(typeof(ShipOrder).Assembly);
        r.Messaging.RegisterHandlersFromAssembly(typeof(ShipOrderHandler).Assembly);
        r.Messaging.Subscribe<ShipOrder>();
    });

    // Routing: the backbone is the event plane; peel the commands off onto the work plane.
    m.DefaultTransport = "events";
    m.Route<ShipOrder>("commands");
});
```

Each transport is configured with its own options object — the same one a standalone RabbitMQ or Kafka
application uses. The new surface is only the routing: a default, plus per-event overrides. Keys are
opaque strings you choose; name them for their role (`"events"`, `"commands"`) rather than their broker.

## The transport map

| Member | Meaning |
|---|---|
| `DefaultTransport` | The key every event without an override is published on. |
| `Route<TEvent>(params keys)` | Route this event to one transport, or several (fan-out). |

The shape is deliberately "one default, a few exceptions": a backbone carries almost everything, a
handful of types are peeled off. Under the hood this is an `EventTransportMap` (in `Messaging.Broker`),
keyed by opaque strings — the same keys the transports were declared under.

## Fan-out: one event to several transports

An event can go to more than one transport at once. The producer still publishes once; the composite
delivers to every mapped bus.

```csharp
m.Route<AuditRecorded>("events", "commands");   // both get it
```

This is why `IEventTransportSelector.SelectFor` returns a **list**: one transport in the common case,
several here. The command handler is unchanged — it never enumerates transports or touches
`GetKeyedService`; that is the composite's job.

**Failure semantics.** A fan-out attempts *every* transport. If one fails, the others still receive the
message, and the failures are gathered into a single `AggregateException` — a Kafka blip does not stop
the RabbitMQ publish. The single-transport case is unchanged: its exception surfaces as-is, no wrapper.

**Atomicity.** Without an outbox a fan-out can partially succeed; consumers are idempotent on `EventId`,
which makes the retry safe. *With* the outbox, each transport's publish writes its own row in the same
transaction, so the fan-out is atomic.

## How many transports, really

The **seam and the routing are N-ary** — the composite resolves any number of keyed buses, and the map
routes and fans out across any of them. `Messaging.Broker` keeps no "register everything" call (see
[brokers.md](./brokers.md#reusing-the-topology-steps)): the composite is one granular piece,
`AddTarsCompositeIntegrationEventBus`, and `AddTarsMassTransitComposite` composes it with the shared broker
core — built once over the union of every transport — exactly the way the RabbitMQ and Kafka providers
compose their own pieces. Routing three-plus transports is just more keys:

```csharp
services.AddTarsMassTransitComposite(m =>
{
    m.AddKafka("events", k => { ... });
    m.AddRabbitMq("commands", r => { ... });
    m.AddRabbitMq<IArchiveBus>("archive", r => { ... });

    m.DefaultTransport = "events";
    m.Route<AuditRecorded>("events", "commands", "archive");   // three-plus is fine
});
```

The limit is **MassTransit's, not the composite's.** In a single process, MassTransit gives one *primary*
bus transport (`UsingRabbitMq`, `UsingAmazonSqs`…) plus riders (Kafka) per `AddMassTransit`. A second
full transport (a second RabbitMQ, and later Azure Service Bus or SQS) is MassTransit **multibus**: a
separate bus, and each bus needs a distinct **type identity** so DI can tell their publish endpoints
apart. That identity is a marker interface — a compile-time type, which is the one thing that cannot be
generated from a runtime key.

So the first full transport is the primary bus, and every additional one carries a marker you declare:

```csharp
public interface IAuditBus : IBus { }   // the marker: this bus's type identity

services.AddTarsMassTransitComposite(m =>
{
    m.AddKafka("events", k => { ... });                 // rider on the primary
    m.AddRabbitMq("commands", r => { ... });            // the primary bus
    m.AddRabbitMq<IAuditBus>("audit", r => { ... });    // a second bus, via multibus

    m.DefaultTransport = "events";
    m.Route<ShipOrder>("commands");
    m.Route<AuditRecorded>("audit");
});
```

`AddRabbitMq<TBus>` wires that transport as its own `AddMassTransit<TBus>` and registers its keyed
`IIntegrationEventBus` against the marker — the composite treats it like any other keyed transport. Add
as many marked buses as you need. The only cost is the one-line marker interface per bus, which is
MassTransit's requirement, surfaced rather than hidden. `MultiTransportInMemoryTests` runs two buses via
exactly this marker pattern. When an SQS provider lands it does the same: `AddAmazonSqs(key, …)` for the
primary or `AddAmazonSqs<TBus>(key, …)` for a multibus bus, contributed by its own package.

Adding a **second primary** (two full transports without a marker) is rejected at startup, because two
buses with no distinct identity are ambiguous:

```
More than one transport wants to be the primary bus ('rabbit-a' (RabbitMQ), 'rabbit-b' (RabbitMQ)). Only
one MassTransit bus can be primary in a process; give the others a marker interface (for example
AddRabbitMq<TBus>) so each becomes its own multibus bus.
```

So: **the model is "as many transports as you want"**, and the physical wiring follows — one primary bus,
riders, and a marked bus per additional full transport. In practice, more brokers usually means more
*services*, each declaring its own subset — but one process can hold several when you need it.

## The end-to-end shape

An event on one plane triggering a job on another, through the one seam:

```csharp
public sealed class PaymentProcessedHandler(IIntegrationEventBus bus)
    : IIntegrationEventHandler<PaymentProcessed>
{
    public Task HandleAsync(PaymentProcessed @event, CancellationToken ct = default)
        => bus.PublishAsync(new ShipOrder(/* ... */ @event.OrderId), ct);   // map routes this to "commands"
}

public sealed class ShipOrderHandler(IShippingClient shipping)
    : IIntegrationEventHandler<ShipOrder>
{
    public Task HandleAsync(ShipOrder @event, CancellationToken ct = default)
        => shipping.DispatchAsync(@event.OrderId, ct);
}
```

`PaymentProcessed` is consumed from Kafka; the handler publishes `ShipOrder` through the composite, which
routes it to RabbitMQ; a RabbitMQ worker consumes it. Neither handler references a broker. This flow, and
a fan-out to two planes from one publish, are covered end-to-end by `MultiTransportInMemoryTests` on
MassTransit's in-memory transport — no Docker.

## Building it by hand

To add a whole new **transport**, the first-class way is to contribute an `IMassTransitBusTransport` or
`IMassTransitRiderTransport` from its own package (that is all `AddRabbitMq`/`AddKafka` do) — the
orchestrator composes it unchanged. `AddTarsMassTransitComposite` is otherwise a composition of public
pieces; nothing is closed. To wire a one-off shape it does not express — a bespoke endpoint, a hand-rolled
bus — assemble the three layers yourself:

1. **The MassTransit registration** — reuse the topology steps from
   [brokers.md](./brokers.md#reusing-the-topology-steps): `bus.UsingRabbitMq(...)` for the primary,
   `bus.AddRider(r => r.UsingKafka(...))` for the rider, and `AddMassTransit<TMarkerBus>` per extra bus
   (what `AddRabbitMq<TBus>` does for you).
2. **Keyed buses** — `AddTarsKeyedRabbitMqIntegrationEventBus(key)` and
   `AddTarsKeyedKafkaIntegrationEventBus(key)`, one per transport.
3. **The composite** — register the shared broker core once over the union of your transports' events and
   handlers (`AddTarsIntegrationEventTypeRegistry` / `…Router` / `…Dispatcher` / `…Handlers`), then
   `AddTarsCompositeIntegrationEventBus(map)` for the map and the composite bus. This is the same
   composition `AddTarsMassTransitComposite` does — no single method registers all of it.

## The outbox with several transports

The [outbox](./outbox.md) is orthogonal — it wraps whichever inner bus publishes. Turn it on through
each transport's options, and keep one `DbContext` with MassTransit's outbox entities. The composite is
**scoped** and resolves the inner bus from its own scope, so the outbox's scoped substitute is the one it
reaches — a root-captured bus would publish straight past it.

## Caveats worth stating

- **Do not subscribe one event type on two transports** unless you mean to consume it twice. Each side
  registers its own relay consumer for its subscriptions.
- **Each transport is a broker to operate.** Connections, health checks, dashboards and failure modes,
  per transport. The provider makes the code clean; it does not remove the operational cost — which is
  why "one broker per application" is the default.

## Main contracts

- `IEventTransportSelector` (returns a list — one transport, or several for fan-out), `EventTransportMap`,
  `EventTransportMapConfiguration` (in `Messaging.Broker`)
- `CompositeIntegrationEventBus` (in `Messaging.Broker`)
- `AddTarsCompositeIntegrationEventBus` (the composite bus and its map) — composed with the shared broker
  core (`AddTarsIntegrationEventTypeRegistry`, `…Router`, `…Dispatcher`, `…Handlers`), never a single
  register-everything call
- `AddTarsKeyedRabbitMqIntegrationEventBus`, `AddTarsKeyedKafkaIntegrationEventBus`
- The composite orchestrator, in the provider-core `Messaging.MassTransit`: `AddTarsMassTransitComposite`,
  `MassTransitCompositeConfiguration` (`Route`, `AddTransport`), and the contributor seam
  `IMassTransitTransport` / `IMassTransitBusTransport` / `IMassTransitRiderTransport`
- The transport contributions, each in its own provider package: `AddRabbitMq`, `AddRabbitMq<TBus>` (in
  `Messaging.MassTransit.RabbitMq`), `AddKafka` (in `Messaging.MassTransit.Kafka`)
