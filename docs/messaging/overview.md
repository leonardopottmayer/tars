# Messaging Overview

## Projects in this family

- `Pottmayer.Tars.Messaging.Abstractions`
- `Pottmayer.Tars.Messaging`

## What the module offers

- a single `IIntegrationEventBus` contract to publish integration events
- `IIntegrationEventHandler<T>` for consumers, with multiple handlers per event
- an in-process bus that dispatches synchronously, in a fresh DI scope
- per-assembly handler scanning and registration
- `IntegrationEventNameAttribute` for a stable, broker-level event name

## Domain event vs. integration event

The module is about **integration events** — facts one module/service publishes for
others to react to. They are different from domain events:

| | Domain event | Integration event |
|---|---|---|
| Scope | inside **one** module | **between** modules (and one day between services) |
| Transport | the mediator, always | `IIntegrationEventBus` (in-process now, broker later) |
| Shape | passes the object reference | broker-ready POCO (JSON), versioned |

Integration events cross boundaries, so they must be **broker-ready POCOs** — plain
`Guid` / `string` / `DateTimeOffset` — and must not leak domain value objects (use
`string Email`, not the `Email` value object). This is what lets the transport be swapped
without touching producers or consumers.

## The transport seam

`IIntegrationEventBus` hides the transport. Today the only implementation is
`InProcessIntegrationEventBus`; when the system splits into services, this bus is the
**only** type replaced by a broker-backed one (RabbitMQ, Kafka, ...). Producers and
consumers do not change.

With a broker, the consuming side re-dispatches the deserialized message to the local
`IIntegrationEventHandler<T>` implementations (the "last mile"), and the broker routes by
the logical `IntegrationEventName`, not by the .NET type — so two services can exchange an
event without sharing the contract assembly.

## Minimal registration

```csharp
builder.Services.AddTarsInProcessIntegrationEventBus(options =>
    options.RegisterHandlersFromAssembly(typeof(AccountActivationRequestedHandler).Assembly));
```

- `AddTarsInProcessIntegrationEventBus` registers `IIntegrationEventBus` as a singleton
  (`TryAdd`, so registering a different bus first wins).
- `RegisterHandlersFromAssembly(assembly, lifetime = Scoped)` scans the assembly and
  registers every concrete `IIntegrationEventHandler<T>` against its closed interface.
- Without the `configure` callback, only the bus is registered; you can register handlers
  yourself or call `AddIntegrationEventHandlersFromAssembly` directly.

## Defining an event

```csharp
[IntegrationEventName("identity.account-activation.v1")] // optional; used by a broker
public sealed record AccountActivationRequested(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    string Email,
    string Token,
    string Locale) : IIntegrationEvent;
```

`EventId` is the stable identity of the occurrence (consumers use it for
idempotency/dedup); `OccurredAt` is the UTC instant it happened.

## Publishing (producer)

Producers publish **after** their unit of work has committed:

```csharp
public sealed class SignUpHandler(IIntegrationEventBus bus, TimeProvider clock)
{
    public async Task HandleAsync(/* ... */ CancellationToken ct)
    {
        // ... persist the user (committed) ...

        await bus.PublishAsync(new AccountActivationRequested(
            EventId: Guid.CreateVersion7(),
            OccurredAt: clock.GetUtcNow(),
            UserId: userId,
            Email: email,
            Token: token,
            Locale: locale), ct);
    }
}
```

## Handling (consumer)

```csharp
public sealed class AccountActivationRequestedHandler(INotificationEnqueuer enqueuer)
    : IIntegrationEventHandler<AccountActivationRequested>
{
    public Task HandleAsync(AccountActivationRequested @event, CancellationToken ct = default)
        => enqueuer.EnqueueActivationEmailAsync(@event.Email, @event.Token, @event.Locale, ct);
}
```

## Dispatch behavior

- Each `PublishAsync` opens a **fresh async DI scope** and resolves the registered
  `IIntegrationEventHandler<T>` for the event's runtime type.
- Handlers run **synchronously**, one after another, within that scope.
- A failing handler is **logged and swallowed**: the producer has already committed and the
  work is re-requestable, so one handler must not abort the others or the caller. If you need
  at-least-once delivery with retry, that belongs in the consumer (e.g. a durable queue), not
  in the bus.

## Main contracts

- `IIntegrationEvent`
- `IIntegrationEventBus`
- `IIntegrationEventHandler<T>`
- `IntegrationEventNameAttribute`
- `IntegrationEventBusOptions`
