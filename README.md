# Pottmayer.Tars

Pottmayer.Tars is a modular .NET framework with reusable building blocks for application development. It is published as a set of small, focused NuGet packages instead of a single monolith — reference only what you use.

## Package groups

- **Core** — `Result`/`Error`/`Optional` primitives, mediator, CQRS helpers, DDD building blocks and localization
- **Caching** — caching abstractions with in-memory and Redis providers
- **Data** — provider-agnostic data abstractions plus the relational provider (EF Core + Dapper), with multi-database and multitenancy support
- **Multitenancy** — tenant resolution pipeline, context, catalog/store and per-tenant execution
- **Web** — HTTP response envelopes, error mapping, response wrapping and pagination (with ASP.NET Core integration)
- **Security / Identity** — JWT issuance/validation, refresh tokens, revocation, magic link and token delivery
- **User Context** — claims-based current-user resolution

> The document-oriented (MongoDB) and GraphQL packages were removed for now and will return as dedicated families. See [docs/data/future-paradigms.md](docs/data/future-paradigms.md).

## Documentation

- [Documentation index](docs/README.md) — capability- and reference-oriented guides per package family
- [Data, Multitenancy and Multi-Database](docs/data/multitenancy-and-multi-database.md) — the in-depth data guide
- [Publishing](docs/reference/publishing.md) — packing and publishing the NuGet packages

## Building and testing

The solution lives at the repository root:

```bash
dotnet build Pottmayer.Tars.slnx
dotnet test Pottmayer.Tars.slnx
```

Unit tests live under `tests/` (one project per family, xUnit + FluentAssertions + Moq).

## Repository

- https://github.com/leonardopottmayer/tars

## Notes

- Some packages are more mature than others in the current `0.0.x` line.
- Review each package description and public API before production use.
