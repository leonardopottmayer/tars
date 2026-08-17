# Broker Transports

> The in-process bus is described in [overview.md](./overview.md). This document covers running the
> same contracts over a real broker.

## Projects

| Project | Level |
|---|---|
| `Pottmayer.Tars.Messaging.Broker` | Runtime — shared by every broker provider |
| `Pottmayer.Tars.Messaging.MassTransit` | Provider core |
| `Pottmayer.Tars.Messaging.MassTransit.RabbitMq` | Transport |
| `Pottmayer.Tars.Messaging.MassTransit.Kafka` | Transport |
| `Pottmayer.Tars.Messaging.MassTransit.EntityFrameworkCore` | Outbox |

Producers and handlers do not reference any of these. They depend on `IIntegrationEventBus` and
`IIntegrationEventHandler<T>`, exactly as they do in-process, and the composition root decides what
is underneath.

## The two-layer model

Brokers do not agree on much. RabbitMQ has exchanges with four routing modes; Kafka has topics and
partitions; Azure Service Bus has topics with SQL filters; SNS has filter policies. Exposing "exchange
type" in the abstraction would be AMQP wearing a generic name, and the day Kafka arrives it becomes a
lie.

So there are two layers:

**The portable core** — what every broker honours, expressed once:

```csharp
public interface IIntegrationEvent          { Guid EventId { get; } DateTimeOffset OccurredAt { get; } }
public interface IRoutedIntegrationEvent    { string RoutingKeySuffix { get; } }   // optional
public interface IHeaderedIntegrationEvent  { IReadOnlyDictionary<string,string> Headers { get; } }  // optional
```

**The provider escape hatch** — what only that broker has, reached through its own options:

```csharp
o.RoutedExchangeType = ExchangeType.Headers;   // RabbitMQ only
o.ConfigureKafka = k => k.SecurityProtocol = ...;   // Kafka only
```

Nothing is hidden. What changes is that the broker-specific parts are visibly broker-specific, so
moving between brokers shows you exactly what will not come along.

## Routing is a property of the event, not of the application

Two shapes, and an application normally has both.

```csharp
// Broadcast — nobody in particular to route to. Whoever cares subscribes.
[IntegrationEventName("identity.password-reset.v1")]
public sealed record PasswordResetRequested(Guid EventId, DateTimeOffset OccurredAt, ...)
    : IIntegrationEvent;

// Keyed — the event knows who it is for.
[IntegrationEventName("inbound.interaction")]
public sealed record InboundInteractionReceived(..., string OwnerModule, string Action)
    : IIntegrationEvent, IRoutedIntegrationEvent
{
    public string RoutingKeySuffix => $"{OwnerModule}.{Action}";
}
```

The published routing key is `{event name}.{suffix}` —
`inbound.interaction.agenda.task_done` — and a subscriber binds a pattern relative to the name:

```csharp
o.Messaging.Subscribe<PasswordResetRequested>();                    // everything under that name
o.Messaging.Subscribe<InboundInteractionReceived>("agenda.#");      // only this module's
```

`*` matches one segment, `#` matches one or more. A wildcard in a *published* key is rejected at
publish time: wildcards belong in subscriptions, and a published key containing one silently matches
nothing.

> **This is not a runtime toggle.** Changing an event from broadcast to keyed changes where it is
> published, so a half-deployed fleet stops seeing itself. Treat it as a topology migration: publish
> to both for a window, move consumers, retire the old route.

## What each broker can actually do

| Capability | RabbitMQ | Kafka | Notes |
|---|---|---|---|
| Broadcast | ✅ fanout | ✅ consumer groups | |
| Keyed routing | ✅ direct | ❌ | see below |
| Wildcard routing | ✅ topic | ❌ | see below |
| Header routing | ✅ headers | ❌ | no broker-side header filtering exists in Kafka |
| Outbox | ✅ | ✅ | through MassTransit's own |

### Why Kafka is broadcast-only here

Not an implementation shortcut. In MassTransit, `AddProducer<T>(topic)` and
`TopicEndpoint<T>(topic, group)` bind **one topic per event type, at registration**. There is no
dynamic-topic producer. A routing key is per *message*, so it has nowhere to live that Kafka can
filter on — it travels as the `tars.routing-key` header and as the message key, which are readable
and give partition affinity, but do not decide delivery.

Emulating it would mean every subscriber reading every message and discarding what is not theirs.
That is not routing; it is broadcast with extra steps, and hiding it behind the abstraction is worse
than not offering it.

So the Kafka provider **rejects a routed subscription at startup**:

```
The subscription to 'inbound.interaction' (pattern 'inbound.interaction.agenda.#') needs
wildcard routing, which Kafka does not support. Change the subscription, or use a broker that
has it.
```

The practical consequence is worth stating plainly: **a design built on directed routing is not
portable from RabbitMQ to Kafka.** That is a fact about Kafka, and it is better to meet it at startup
than in production.

## Registration

RabbitMQ:

```csharp
services.AddTarsMassTransitRabbitMq(o =>
{
    o.Host = "localhost";
    o.Username = "guest";
    o.Password = "guest";

    o.Messaging.EndpointName = "channels";                      // the queue
    o.Messaging.RegisterEventsFromAssembly(typeof(Events).Assembly);
    o.Messaging.RegisterHandlersFromAssembly(typeof(Handlers).Assembly);
    o.Messaging.Subscribe<InboundInteractionReceived>("agenda.#");
});
```

Kafka — same shape, and the same producers and handlers:

```csharp
services.AddTarsMassTransitKafka(o =>
{
    o.BootstrapServers = "localhost:9092";

    o.Messaging.EndpointName = "channels";                      // the consumer group
    o.Messaging.RegisterEventsFromAssembly(typeof(Events).Assembly);
    o.Messaging.RegisterHandlersFromAssembly(typeof(Handlers).Assembly);
    o.Messaging.Subscribe<PasswordResetRequested>();
});
```

`EndpointName` is the one portable identity: a queue on RabbitMQ, a consumer group on Kafka.
Instances sharing it compete for messages; different values each get a copy.

## Composition

`Messaging.Broker` registers **one service per method** and has no "register everything" call. A
provider composes exactly what it uses, which is what makes each piece replaceable without forking
the provider.

| Method | Registers |
|---|---|
| `AddTarsIntegrationEventTypeRegistry(types)` / `(assemblies)` | The name-to-type map |
| `AddTarsIntegrationEventRouter()` / `<TRouter>()` | How an event becomes a route |
| `AddTarsIntegrationEventDispatcher()` / `<TDispatcher>()` | The last mile |
| `AddTarsIntegrationEventHandlers(assembly, lifetime)` | The `IIntegrationEventHandler<T>` implementations |

A provider does this, and nothing more:

```csharp
services.AddTarsIntegrationEventTypeRegistry(options.Messaging.DiscoverEventTypes());
services.AddTarsIntegrationEventRouter();
services.AddTarsIntegrationEventDispatcher();

foreach (var (assembly, lifetime) in options.Messaging.HandlerAssemblies)
    services.AddTarsIntegrationEventHandlers(assembly, lifetime);
```

Every registration is `TryAdd`, so an application that registers a replacement **first** keeps it:

```csharp
services.AddTarsIntegrationEventRouter<TenantPrefixedRouter>();   // wins
services.AddTarsMassTransitRabbitMq(o => { ... });                 // does not overwrite it
```

That is how a house convention — a tenant prefix, an environment segment — applies across every
provider without any of them knowing about it.

### Options

`BrokerMessagingOptions` binds from `Tars:Messaging:Broker`:

```csharp
builder.AddTarsBrokerMessagingOptions();
```

Only `EndpointName` comes from configuration — it is the one value that legitimately differs per
environment. Subscriptions and assemblies stay in code: a subscription is a compile-time relationship
between a handler and an event, and moving it into `appsettings` turns a build error into a queue
that silently never fills. Use the `configure` callback for those.

In practice a provider carries its own options object (`TarsRabbitMqOptions.Messaging`), so binding
this separately is only needed when composing the broker core by hand.

### Reusing the topology steps

Each provider's registration is a composition of public, single-purpose steps. Nothing is all or
nothing: an application that needs a bus configuration the options do not express writes its own
MassTransit block and keeps the Tars naming, exchanges and bindings.

**RabbitMQ** — `RabbitMqTopology`:

| Method | Extends |
|---|---|
| `AddTarsRelayConsumers(subscriptions)` | `IBusRegistrationConfigurator` |
| `UseTarsHost(host, port, vhost, user, pass, ssl)` | `IRabbitMqBusFactoryConfigurator` |
| `UseTarsEntityNaming()` | " |
| `UseTarsPublishTopology(eventTypes, exchangeType)` | " |
| `BindTarsSubscription(s)` / `BindTarsSubscriptions(...)` | `IRabbitMqReceiveEndpointConfigurator` |
| `UseTarsRetry(limit, interval)` | " |

**Kafka** — `KafkaTopology`:

| Method | Extends |
|---|---|
| `AddTarsProducer(s)(eventTypes)` | `IRiderRegistrationConfigurator` |
| `AddTarsRelayConsumers(subscriptions)` | " |
| `UseTarsTopicEndpoint(s)(...)` | `IKafkaFactoryConfigurator` |

```csharp
services.AddMassTransit(bus =>
{
    bus.AddTarsRelayConsumers(subscriptions);

    bus.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseTarsEntityNaming();                                   // keep the logical naming
        cfg.UseTarsPublishTopology(eventTypes, ExchangeType.Topic);

        cfg.ReceiveEndpoint("my-queue", (IRabbitMqReceiveEndpointConfigurator e) =>
        {
            e.BindTarsSubscriptions(subscriptions, ExchangeType.Topic);
            e.ConfigureConsumers(context);
            // ...and anything else you want
        });
    });
});
```

The per-service registrations compose the same way:

| Method | Registers |
|---|---|
| `AddTarsRabbitMqIntegrationEventBus()` | the MassTransit-backed bus |
| `AddTarsRabbitMqRouteApplier()` | the routing key applier |
| `AddTarsKafkaIntegrationEventBus()` | the Kafka bus (its own, since the rider has no publish endpoint) |

Both buses are registered **scoped**, because what they publish through is: MassTransit's
`IPublishEndpoint` and `ITopicProducer<T>` are scoped, and the outbox works by giving the scope a
substitute that writes to the outbox tables instead of to the broker. A singleton bus would capture
the root one and publish straight past the outbox — configured, and silently storing nothing. So
resolve `IIntegrationEventBus` from a scope, which is where a request or a consume already is.

### One broker per application

The bus has one `PublishAsync`, so one provider is registered and everything goes there. Most systems
use one broker, and this keeps "swap the broker" a one-line change. Running two at once would need a
named-bus concept, which is deliberately not built until something needs it.

## Failure behaviour

The last mile — `ScopedIntegrationEventDispatcher` — resolves handlers in a fresh scope and
**propagates the first exception**, unlike the in-process bus, which logs and swallows. That is the
point of a broker: a failure has to reach the transport so retry and dead-lettering can act.
Swallowing would turn a durable queue back into fire-and-forget.

Consequences to design for:

- Delivery is **at-least-once**. On a retry every handler runs again, including the ones that already
  succeeded. Handlers must be idempotent; `EventId` is the deduplication key they are given for
  exactly that.
- An event with **no registered handler** is acknowledged and dropped, not failed. A queue can
  legitimately receive something this service does not act on. It is logged at debug, because the
  usual cause is a handler nobody registered.

## Naming

Broker entities are named after the event's **logical name**, never the .NET type:

```csharp
[IntegrationEventName("identity.password-reset.v1")]
```

MassTransit's default formatter derives the exchange name from namespace and class name, so moving a
record to another namespace would quietly repoint it at a new exchange and existing consumers would
go silent. `TarsEntityNameFormatter` replaces that so the route belongs to the contract.

Without the attribute the name falls back to kebab-case of the type name
(`PasswordResetRequested` → `password-reset-requested`). That keeps things working with no ceremony,
but it ties the wire name to an identifier: **declare the attribute on anything that crosses a
service boundary or that you intend to version.**

Two types resolving to the same name is a **startup failure**, not a runtime surprise — otherwise one
of them silently never gets delivered and which one is undefined.

## Outbox

Publishing after a commit is a second commit that can fail on its own, and the event vanishes with
nobody noticing. The outbox writes the message in the *same transaction* as the state change, and a
relay delivers it afterwards.

```csharp
services.AddTarsMassTransitRabbitMq(o =>
{
    o.Host = "localhost";
    o.UseEntityFrameworkOutbox<TarsRabbitMqOptions, AppDbContext>(x => x.UsePostgres());
});
```

This is **MassTransit's** outbox, not a third implementation — the framework we already depend on has
one, and wrapping our own around it would be abstraction over abstraction. The wrapper adds exactly
two things: it works the same for every broker, and it always calls `UseBusOutbox()`, without which
the outbox stores nothing on publish and looks configured while doing nothing.

`AppDbContext` must include MassTransit's outbox entities (`InboxState`, `OutboxMessage`,
`OutboxState`) and a migration creating them. That is deliberate: the whole point is that those rows
are written by the application's own transaction, so they live in the application's own context.

## What is not covered

**Silverback.** Evaluated and set aside. Silverback v5 dropped RabbitMQ: `Silverback.Core`,
`.Integration` and `.Integration.Kafka` are on the 5.x line, while `Silverback.Integration.RabbitMQ`
is frozen at `4.7.0` (netstandard2.1). So "Silverback for RabbitMQ and Kafka" cannot sit on a current
version — 4.7.0 gives both brokers on a stale line, 5.4.2 gives Kafka without RabbitMQ — and the v4
and v5 configuration APIs differ too much for one core package to span both. `Messaging.Broker`
isolates the framework choice, so adding it later touches no producer and no handler.

**Azure Service Bus, SQS.** No provider yet. Both fit the portable model — they have broadcast, keyed
and header-based filtering — so they would be a transport package alongside the existing two.

**Named buses.** Running two brokers in one application. Not built until something needs it.

## Main contracts

- `IIntegrationEventBus`, `IIntegrationEventHandler<T>`, `IIntegrationEvent` (unchanged)
- `IRoutedIntegrationEvent`, `IHeaderedIntegrationEvent`, `IntegrationEventNaming`
- `IntegrationEventRoute`, `IIntegrationEventRouter`, `IntegrationEventSubscription`
- `IIntegrationEventTypeRegistry`, `IIntegrationEventDispatcher`
- `BrokerCapabilities`, `BrokerMessagingOptions`
