# Pottmayer.Tars Documentation

This folder gathers the functional documentation of the `Pottmayer.Tars` framework, organized by project family. The structure is designed to allow two reading modes:

- capability-oriented reading: "I want to configure identity, data, localization, multitenancy, web"
- reference-oriented reading: "I want to know which package contains a contract, an `AddTars*`, an option or an adapter"

## How to navigate

- [Core](./core/overview.md): primitives, mediator, CQRS, DDD and localization
  - [Localization](./core/localization.md): `IMessageProvider`, `IMessageSource`, `InMemoryMessageSource`, `.resx`, `IStringLocalizer`, `appsettings`
- [Caching](./caching/overview.md): abstractions, memory, redis, serializer, key builder
- [Data](./data/overview.md): contracts, contexts, pipelines, unit of work, unified EF Core + Dapper, multi-database, domain events
  - [Configuration (Relational)](./data/configuration.md): appsettings, multi-database, multitenancy, custom resolver
  - [Contracts and UoW](./data/pipelines-and-uow.md): `IUnitOfWork`, `IDataContext`, repositories, `QueryParams`, domain events
  - [Data, Multitenancy and Multi-Database](./data/multitenancy-and-multi-database.md): complete guide to logical keys, connection resolution, per-scenario combinations and cross-database transaction coordination
  - [Future paradigms](./data/future-paradigms.md): Document (MongoDB), Key-Value, Search
- [Multitenancy](./multitenancy/overview.md): tenant context, resolution pipeline, catalog, store, per-tenant execution
  - [Resolvers](./multitenancy/resolvers.md): all built-in resolvers and how to create a custom one
  - [Catalog and Store](./multitenancy/catalog-and-store.md): `ITenantCatalog`, `ITenantStore`, implementations
  - [Per-tenant execution](./multitenancy/execution.md): `ITenantExecutionRunner`, workers, jobs, manual scope
  - [ASP.NET Core](./multitenancy/aspnetcore.md): middleware, HTTP resolvers, pipeline order, filters
  - [Data isolation](./multitenancy/data-isolation.md): `ITenantConnectionStringProvider`, `ITenantSchemaProvider`, strategies
  - [Configuration](./multitenancy/configuration.md): appsettings, full DI, scenarios
  - [Data, Multitenancy and Multi-Database](./data/multitenancy-and-multi-database.md): how to combine tenant resolution with data resolution, multiple databases and transactions
- [Web](./web/overview.md): HTTP envelopes, error mapping, exception filter, wrapping and pagination
  - [HTTP packages and configuration](./web/http.md): DI, appsettings, per-scenario composition
  - [HTTP Error Mapping](./web/error-mapping.md): `IHttpErrorMapper`, `DefaultHttpErrorMapper`, `TarsExceptionFilter`
  - [Response Wrapping](./web/response-wrapping.md): controllers, Minimal APIs, attributes and metadata
  - [Result Extensions and pagination](./web/result-extensions.md): `ToHttpResult()` and `WritePaginationHeaders()`
- [Security / Identity](./security/identity/overview.md): JWT, refresh token, revocation, magic link, API key, OAuth, HTTP transport
  - [Configuration](./security/identity/configuration.md): appsettings, all modes and scenarios
  - [Registration scenarios (DI)](./security/identity/scenarios.md): from minimal to complete
  - [Authentication flows](./security/identity/authentication-flows.md): `IPasswordAuthenticator`, magic link, API key, refresh
  - [Endpoints](./security/identity/endpoints.md): built-in and custom endpoints, and tests with cURL
  - [Token delivery](./security/identity/token-delivery.md): header, cookie, body, hybrid, custom transport
  - [OAuth](./security/identity/oauth.md): Google, GitHub, Microsoft and external providers
- [User Context](./user-context/overview.md): user resolution from claims
  - [Typed system](./user-context/typed-context.md): `IUserContext<TUser>`, claim mapping, resolver, fallback provider
  - [Claims system](./user-context/claims-context.md): `IUserContext`, `AsyncLocalUserContextAccessor`, middleware, workers, Blazor
  - [Configuration](./user-context/configuration.md): appsettings, `UserContextOptions`, lifetimes, fallback
  - [Registration scenarios (DI)](./user-context/scenarios.md): from minimal to complete
  - [Testing](./user-context/testing.md): direct injection, DI, integration, reusable helpers
- [Reference](./reference/package-matrix.md): global maps, application blueprint and crosswalk with the example apps
  - [Package taxonomy](./reference/taxonomy.md): the four levels (Abstractions, Runtime, Provider, Host Integration) and naming rules
  - [Publishing](./reference/publishing.md): packaging and publishing the NuGet packages (GitHub Packages), versioning and scripts

## Architectural principles of the repository

The repository is organized to separate:

- agnostic packages: rules, contracts, reusable services, builders and components that work outside ASP.NET Core
- `*.AspNetCore` packages: middleware, endpoints, filters, `HttpContext` read/write, HTTP pipeline integration

In practical terms:

- `Pottmayer.Tars.Core.Localization` contains the message runtime
- `Pottmayer.Tars.Core.Localization.AspNetCore` integrates per-request culture and `IStringLocalizer`
- `Pottmayer.Tars.Web.Http` contains the framework's HTTP core
- `Pottmayer.Tars.Web.Http.AspNetCore` contains the MVC and Minimal API filters
- `Pottmayer.Tars.Security.Identity` contains the identity core
- `Pottmayer.Tars.Security.Identity.AspNetCore` contains the endpoints and HTTP transport
- `Pottmayer.Tars.UserContext` contains the agnostic user resolution
- `Pottmayer.Tars.UserContext.AspNetCore` contains the `HttpContext`-based accessor
- `Pottmayer.Tars.Multitenancy` contains the tenant pipeline
- `Pottmayer.Tars.Multitenancy.AspNetCore` contains the middleware and HTTP resolvers

## Example applications used as a baseline

This documentation was cross-checked against two real consumers of the framework:

- `pottmayer-pandora/pandora-app-backend`
- `roberto/roberto-backend`

They are especially important because they show:

- real DI composition through the application's adapters
- real configuration in `appsettings`
- combined use of `Data`, `Identity`, `UserContext`, `Caching` and web concerns
- multitenancy in a complete host in the case of `Roberto`

See [Example Apps Crosswalk](./reference/example-apps-crosswalk.md) to locate each capability in those consumers.

## What is out of scope for this folder

- business documentation of the consumer apps
- deployment tutorial
- version migration guide

## Legacy documents

The `docs/presentation` folder was kept only as a bridge for the `Presentation.Rest` -> `Web.Http` rename. For current documentation, always prefer `docs/web`.
