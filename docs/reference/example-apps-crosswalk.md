# Example Apps Crosswalk

## Goal

This document points out where each framework capability appears in the example apps used to validate the documentation.

## Note about naming and paths

Both example apps have been reorganized since this crosswalk was first written. Current locations:

- **Pandora**: `G:\dev\pandora\backend\src` (repo root `G:\dev\pandora`) — a modular monolith, one project family per module (`Modules/<Name>/Pottmayer.Pandora.Modules.<Name>.{Domain,Application,Infrastructure,Persistence,Presentation}`) plus `Shared/` and `Host/`. It does **not** use an `App.Adapter.*` layout.
- **Roberto**: `G:\dev\exodyas\roberto\backend\src` (repo root `G:\dev\exodyas\roberto`) — `Exodyas.Roberto.{Domain,Application,Infrastructure,Persistence,Presentation,Host}`.

`Presentation.Rest` in older framework versions corresponds to the current role of `Web.Http`.

Use this crosswalk as a reference for real composition, not as a literal mirror of file layout — module lists change as the apps evolve.

## Pandora

### Main host

- [`Host/Pottmayer.Pandora.Host/Program.cs`](../../../pandora/backend/src/Host/Pottmayer.Pandora.Host/Program.cs)
- Composes Observability (via Shared), Localization, UserContext, Web.Http, per-module Persistence/Infrastructure/Application/Presentation, and the outbox (via `OutboxRegistration.cs`).

### Data

- Per module: `Modules/<Name>/Pottmayer.Pandora.Modules.<Name>.Persistence/DI/`
- Shared interceptors/value converters: [`Shared/Pottmayer.Pandora.Shared.Persistence`](../../../pandora/backend/src/Shared/Pottmayer.Pandora.Shared.Persistence)

### Identity

- `Modules/Identity/Pottmayer.Pandora.Modules.Identity.{Application,Infrastructure,Persistence,Presentation}/DI/`
- Shows identity options, JWT issuance, refresh token and ASP.NET Core transport, composed per module rather than in one adapter project.

### Web

- Per module: `Modules/<Name>/Pottmayer.Pandora.Modules.<Name>.Presentation/DI/`
- Shared HTTP/wrapping concerns: [`Shared/Pottmayer.Pandora.Shared.Infrastructure`](../../../pandora/backend/src/Shared/Pottmayer.Pandora.Shared.Infrastructure)

### User Context

- [`Shared/Pottmayer.Pandora.Shared.Infrastructure/DI/SharedInfrastructureDI.cs`](../../../pandora/backend/src/Shared/Pottmayer.Pandora.Shared.Infrastructure/DI/SharedInfrastructureDI.cs)
- Shows user resolution, accessor and fallback provider, registered once for the whole host.

### Core

- Per module: `Modules/<Name>/Pottmayer.Pandora.Modules.<Name>.Application/DI/`
- Shows mediator, CQRS and the exception-mapping behavior, registered per module rather than globally.

### Caching

- **Not currently used.** No `AddTarsMemoryCache*`/`AddTarsRedis*` call exists in the Pandora backend as of this writing.

### Observability

- [`Shared/Pottmayer.Pandora.Shared.Infrastructure/DI/SharedInfrastructureDI.cs`](../../../pandora/backend/src/Shared/Pottmayer.Pandora.Shared.Infrastructure/DI/SharedInfrastructureDI.cs)
- `Program.cs` calls `builder.AddPandoraSharedInfrastructure()`, which registers Observability alongside other shared concerns — see the `AddTarsObservability*` calls inside that file for the concrete signal composition.

### Messaging (in-process outbox)

- [`Host/Pottmayer.Pandora.Host/OutboxRegistration.cs`](../../../pandora/backend/src/Host/Pottmayer.Pandora.Host/OutboxRegistration.cs)
- The only messaging transport Pandora uses: `Pottmayer.Tars.Messaging.EntityFrameworkCore`'s in-process outbox, one relay per producing database (Identity, Channels, Agenda, Integrations). No broker is registered. See [Transactional Outbox](../messaging/outbox.md).

### Communication

- [`Modules/Channels/Pottmayer.Pandora.Modules.Channels.Infrastructure/DI/InfrastructureDI.cs`](../../../pandora/backend/src/Modules/Channels/Pottmayer.Pandora.Modules.Channels.Infrastructure/DI/InfrastructureDI.cs)
- The Channels module owns email/Telegram wiring — it is the only module that talks to `Communication.*`.

## Roberto

### Main host

- [`Host/Program.cs`](../../../exodyas/roberto/backend/src/Exodyas.Roberto.Host/Program.cs)
- Beyond the base set, it shows `Multitenancy`.

### Multitenancy

- `Host/Program.cs`
- `Host/Multitenancy/` (tenant catalog and resolvers)
- Shows the tenant pipeline, catalog and HTTP/custom resolvers.

### Multi-database data

- `Persistence/DI/`, `Persistence/Modules/`, `Persistence/EFCore/`
- Shows the `default` pipeline, an additional `central` pipeline and per-module repositories.

### Workers

- `Infrastructure/Workers/`
- Show usage scenarios for the mediator, data and multitenancy outside the HTTP flow.

### Caching, Messaging, Communication, Observability

- **Not currently used** in Roberto. If you need a worked multitenancy + these-families example, none of the two example apps currently provides one — compose from the family's own configuration guide directly.

## How to use this crosswalk

- If you want an example of a complete, simple HTTP backend composition with the in-process outbox: start with `Pandora`.
- If you want an example with multitenancy and more than one logical database: use `Roberto`.
- If you want Caching, or a broker-backed Messaging transport (RabbitMQ/Kafka): neither example app uses them yet — follow [Caching configuration](../caching/configuration.md) or [Messaging configuration](../messaging/configuration.md) directly.
- If you want to validate the framework's architectural conventions: compare the equivalent module/adapter projects of the two apps.
