# Coding Conventions

Code-level idioms every Tars package follows. These are not style preferences — they are decisions
already made and applied consistently across the framework, collected here so new code matches the old.

Package-level rules (the four levels, project naming, folder = namespace) live in
[taxonomy.md](./taxonomy.md); this document is about classes and methods **inside** a package.

## `Options` vs `Configuration` suffix

The suffix records **how the type is configured**, so the name tells you whether appsettings is in play.

| Suffix | Means | Test |
|---|---|---|
| `…Options` | Binds to an appsettings section | Has an `IHostApplicationBuilder` + `sectionName` overload that reads a `Tars:…` section |
| `…Configuration` | Configured only in code, via `Action<>` | No section, no binder — a builder you call methods on |

- `MassTransitRabbitMqMessagingOptions`, `MassTransitKafkaMessagingOptions` bind `Tars:Messaging:RabbitMq` /
  `:Kafka` (host, port, credentials differ per environment) → `Options`.
- `MassTransitCompositeConfiguration`, `EventTransportMapConfiguration` are code-only builders — they
  expose methods (`AddRabbitMq<TBus>`, `Route<T>`, closures) that JSON cannot express → `Configuration`.

A trio like `…RabbitMqMessagingOptions` / `…KafkaMessagingOptions` / `…MultiMessagingConfiguration`
breaking symmetry is **correct**: the different suffix signals the last one is not (and cannot be)
appsettings-bound.

## One method per registration

There is **no "register everything" method** in a Runtime or provider-core library. Each registration is
its own small, public, independently callable method; the caller composes exactly what it uses.

> `BrokerMessagingServicesDI` states it in its own doc: *"The pieces every broker provider needs,
> registered one at a time. There is deliberately no 'register everything' method."*

Only a **provider entry point** composes — and even it is *"a composition of the smaller methods, nothing
more; every step is public and callable on its own"* (see `AddTarsMassTransitRabbitMq`). If a method
configures "a whole thing", split it: expose the pieces, let the entry point call them in turn, and let
the docs show how to combine them.

```csharp
// Provider entry point — composes public pieces, nothing hidden:
services.AddTarsIntegrationEventTypeRegistry(eventTypes);
services.AddTarsIntegrationEventRouter();
services.AddTarsIntegrationEventDispatcher();
foreach (var (asm, life) in handlerAssemblies)
    services.AddTarsIntegrationEventHandlers(asm, life);
```

## `TryAdd` so a consumer can override

Registrations use `TryAdd*` (not `Add*`), so an application that registers its own implementation
**first** wins and the provider leaves it in place. This is how a consumer imposes its own convention
without forking a provider:

```csharp
services.AddTarsIntegrationEventRouter<TenantPrefixedRouter>();   // registered first
services.AddTarsMassTransitRabbitMq(o => …);                       // keeps the router above
```

## `AddTars*` naming, and `AddTarsKeyed*` for keyed

Public registration extensions are named `AddTars<Thing>`. A keyed-DI variant puts **`Keyed` right after
`AddTars`** (infix), not as a suffix: `AddTarsKeyedRabbitMqIntegrationEventBus`, not `…EventBusKeyed`.

This matches the ecosystem and the repo: Microsoft's `AddKeyedScoped`/`AddKeyedSingleton`, and Tars's own
`IKeyedDataContextFactory` / `KeyedAiChatCompletionClientFactory` — "keyed" qualifies the registration, so
it sits before the noun. The keyed bus is the *same* type registered in a keyed slot; the name should not
imply a distinct `…BusKeyed` artifact.

## Fail fast at startup

Validate options and composition **at registration**, not at first use — a misconfiguration should stop
the host from starting, not surface on the first publish in production.

- Options validate themselves (`IsValid()` / `ValidationErrorMessage`) and against their broker's
  capability matrix (`ValidateAgainst(BrokerCapabilities.Amqp, "RabbitMQ")`).
- Composition validates cross-references (`ValidateComposition`: a route to an undeclared transport, or an
  event routed to a transport that has no producer for it).
- Required invariants throw in the constructor (`EventTransportMap` rejects a missing default transport).

The error message names the fix (which method to call, which assembly to register), so the failure is
actionable at the call site.
