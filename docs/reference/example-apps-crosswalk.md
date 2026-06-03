# Example Apps Crosswalk

## Goal

This document points out where each framework capability appears in the example apps used to validate the documentation.

## Note about naming

The example apps still reflect part of the framework's previous naming. In particular:

- `Presentation.Rest` in the consumers corresponds to the current role of `Web.Http`
- localization concerns may appear embedded in host adapters, even when the `Core.Localization` family was not yet isolated

Use this crosswalk as a reference for real composition, not as a literal mirror of the current package names.

## Pandora

### Main host

- `pottmayer-pandora/pandora-app-backend/src/App/Pottmayer.Pandora.App.Host/Program.cs`
- Shows `Caching`, `Authentication`, `Authorization`, controllers, Swagger and identity endpoint mapping.

### Data

- `.../Pottmayer.Pandora.App.Adapter.Data/DI/PandoraDataAdapterDI.cs`
- Shows data composition, pipelines and repositories.

### Identity

- `.../Pottmayer.Pandora.App.Adapter.Identity/DI/PandoraIdentityAdapterDI.cs`
- Shows identity options, JWT issuance, refresh token and ASP.NET Core transport.

### Web

- `.../Pottmayer.Pandora.App.Adapter.Rest/DI/PandoraRestAdapterDI.cs`
- Corresponds today to the role of the `Web.Http` adapter.
- Shows:
  - HTTP layer options
  - wrapping concerns
  - filters and MVC configuration

### User Context

- `.../Pottmayer.Pandora.App.Adapter.UserContext/DI/PandoraUserContextAdapterDI.cs`
- Shows user resolution, accessor and fallback provider.

### Core

- `.../Pottmayer.Pandora.App.Core.Application/DI/PandoraApplicationDI.cs`
- Shows mediator, CQRS and the exception-mapping behavior.

## Roberto

### Main host

- `roberto/roberto-backend/src/Exodyas.Roberto.App.Host/Program.cs`
- Beyond the base set, it shows `Multitenancy`.

### Multitenancy

- `.../Program.cs`
- `.../Host/Multitenancy/ConfigurationTenantCatalog.cs`
- `.../Host/Multitenancy/RobertoHostTenantResolver.cs`
- Shows the tenant pipeline, catalog and HTTP/custom resolvers.

### Multi-database data

- `.../Adapter.Data/DI/RobertoDataAdapterDI.cs`
- Shows the `default` pipeline, an additional `central` pipeline and repositories.

### Workers

- `.../Adapter.Workers/...`
- Show usage scenarios for the mediator, data and multitenancy outside the HTTP flow.

## How to use this crosswalk

- If you want an example of a complete, simple HTTP backend composition: start with `Pandora`.
- If you want an example with multitenancy and more than one logical database: use `Roberto`.
- If you want to validate the framework's architectural conventions: compare the equivalent adapters of the two apps, remembering the `Presentation.Rest` -> `Web.Http` equivalence.
