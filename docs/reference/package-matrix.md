# Package Matrix

## Overview

Table of the `Pottmayer.Tars` projects with their architectural level, usage classification and role.

See [taxonomy.md](./taxonomy.md) for the complete definition of the levels and classifications.

## Core

| Project | Level | Classification | Role |
|---|---|---|---|
| `Pottmayer.Tars.Core.Primitives` | Abstractions | Essential | `Result`, `Error`, `Optional<T>`, utility extensions |
| `Pottmayer.Tars.Core.Mediator.Abstractions` | Abstractions | Optional | mediator, request, notification and pipeline contracts |
| `Pottmayer.Tars.Core.Mediator` | Runtime | Optional | mediator implementation and per-assembly DI scanners |
| `Pottmayer.Tars.Core.Cqrs` | Runtime | Architectural style | bases for commands/queries and the exception-mapping behavior |
| `Pottmayer.Tars.Core.Ddd` | Abstractions | Architectural style | `Entity`, `AggregateRoot`, domain events, dispatcher |
| `Pottmayer.Tars.Core.Localization.Abstractions` | Abstractions | Optional | `IMessageProvider`, `IMessageSource` |
| `Pottmayer.Tars.Core.Localization` | Runtime | Optional | message source composition, `InMemoryMessageSource`, `ResourceManagerMessageSource` |
| `Pottmayer.Tars.Core.Localization.AspNetCore` | Host Integration | Optional | `RequestLocalizationMiddleware`, `IStringLocalizer`, culture options |

## Caching

| Project | Level | Classification | Role |
|---|---|---|---|
| `Pottmayer.Tars.Caching.Abstractions` | Abstractions | Optional | contracts (`ICacheStore`, `ICacheSerializer`, `ICacheKeyBuilder`) |
| `Pottmayer.Tars.Caching.Core` | Runtime | Optional | default JSON serializer, default key builder, `CacheOptions` |
| `Pottmayer.Tars.Caching.Memory` | Provider | Optional | provider over `IMemoryCache` |
| `Pottmayer.Tars.Caching.Redis` | Provider | Optional | Redis provider and connection options |

## Data — Shared contracts

| Project | Level | Classification | Role |
|---|---|---|---|
| `Pottmayer.Tars.Data.Abstractions` | Abstractions | Essential (data axis) | provider-agnostic contracts: `IUnitOfWork`, `IUnitOfWorkFactory`, `IDataContext`, `IDataContextAccessor`, `IRepository`, `IRepositoryResolver`, `QueryParams`, `FilterSpec`, `FilterOperator`, `SortOption`, `DataQueryResult`, `DataKeys` |

## Data — Relational Axis

| Project | Level | Classification | Role |
|---|---|---|---|
| `Pottmayer.Tars.Data.Relational.Abstractions` | Abstractions | Optional | relational contracts: `IStandardRepository`, `IDataContextFactory`, `IDataConnectionResolver`, `IDataConnectionDescriptor`, `IMultiDatabaseCoordinator`, `ITenantConnectionStringProvider`, `ITenantSchemaProvider` |
| `Pottmayer.Tars.Data.Relational` | Runtime | Optional | EF Core + Dapper implementation, `RelationalDbContext`, `DataContext`, `StandardRepository`, `DataContextAccessor`, unified DI |

> The document axis (MongoDB) was temporarily removed and will return as a dedicated family (`Data.Document.*`). See [future paradigms](../data/future-paradigms.md).

## Data — Legacy packages

> Kept only as migration history. New projects should use `Data.Abstractions` + `Data.Relational.Abstractions` + `Data.Relational`.

| Project | Status | Replacement |
|---|---|---|
| `Pottmayer.Tars.Data.Core` | Obsolete | merged into `Data.Relational` |
| `Pottmayer.Tars.Data.EFCore` | Obsolete | merged into `Data.Relational` |
| `Pottmayer.Tars.Data.Dapper` | Obsolete | merged into `Data.Relational` |
| `Pottmayer.Tars.Data.Hybrid` | Obsolete | default behavior of `Data.Relational` |

## Multitenancy

| Project | Level | Classification | Role |
|---|---|---|---|
| `Pottmayer.Tars.Multitenancy.Abstractions` | Abstractions | Optional | tenant context, resolver, pipeline, catalog, store and execution contracts |
| `Pottmayer.Tars.Multitenancy` | Runtime | Optional | resolution pipeline, agnostic resolvers, in-memory catalog, store, per-tenant execution |
| `Pottmayer.Tars.Multitenancy.AspNetCore` | Host Integration | Optional | middleware and HTTP resolvers (`header`, `subdomain`) |

## Web

| Project | Level | Classification | Role |
|---|---|---|---|
| `Pottmayer.Tars.Web.Http.Abstractions` | Abstractions | Optional | HTTP contracts, envelopes, wrap decision, pagination |
| `Pottmayer.Tars.Web.Http` | Runtime | Optional | `HttpResponse`, `HttpErrorResponse`, `DefaultHttpErrorMapper`, `WrapDecisionService` |
| `Pottmayer.Tars.Web.Http.AspNetCore` | Host Integration | Optional | MVC/Minimal API filters, exception filter, HTTP extensions, options |

## Security / Identity

| Project | Level | Classification | Role |
|---|---|---|---|
| `Pottmayer.Tars.Security.Identity.Abstractions` | Abstractions | Optional | DTOs, authentication contracts, stores, token services, transport contracts |
| `Pottmayer.Tars.Security.Identity` | Runtime | Optional | JWT issuance/validation, refresh, revocation, magic link, core policies |
| `Pottmayer.Tars.Security.Identity.AspNetCore` | Host Integration | Optional | endpoints, HTTP transport, JWT bearer, API key, OAuth |

## User Context

| Project | Level | Classification | Role |
|---|---|---|---|
| `Pottmayer.Tars.UserContext.Abstractions` | Abstractions | Optional | user context contracts |
| `Pottmayer.Tars.UserContext` | Runtime | Optional | `UserContext`, `AsyncLocalUserContextAccessor`, agnostic resolution |
| `Pottmayer.Tars.UserContext.AspNetCore` | Host Integration | Optional | middleware and bridge with `HttpContext` |

## Important notes

- The framework publishes multiple small packages instead of a single monolith.
- `Data.Abstractions` is the base package of the entire data family — the relational axis depends on it.
- `Web.Http` replaces the old `Presentation.Rest` family.
- When using the relational axis, reference `Data.Relational.Abstractions` + `Data.Relational`, which reference `Data.Abstractions` automatically.
- When a `*.AspNetCore` package exists, it is the correct place for middleware, endpoints, filters and integration with the specific host.
