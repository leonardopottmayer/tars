<p align="center">
  <img src="docs/assets/logo.png" alt="Tars" width="220">
</p>

# Pottmayer.Tars

Pottmayer.Tars is a modular .NET framework of reusable building blocks for backend applications: primitives and mediator, data access, messaging, identity, caching, observability and more. It ships as **53 small, focused NuGet packages** instead of a single monolith, so an application references only what it uses.

- **Granular by design.** Each family is split into `Abstractions`, a runtime and one or more providers or host integrations, so contracts never drag an implementation (or ASP.NET Core) along.
- **One method per registration.** Every piece has its own `AddTars*` extension, registered with `TryAdd` so the host can override it, and options are validated at startup.
- **Documented per family.** Each family has its own guide under [`docs/`](docs/README.md).

Requires **.NET 10**.

## Package families

| Family | What it gives you | Docs |
|---|---|---|
| **Core** | `Result`/`Error`/`Optional` primitives, mediator with pipeline behaviors, CQRS helpers, DDD building blocks, localization | [Core](docs/core/overview.md) |
| **Data** | Provider-agnostic repositories and unit of work; relational provider (EF Core + Dapper) and MongoDB provider; multi-database coordination and multitenant connections | [Data](docs/data/overview.md) |
| **Messaging** | Integration events with an in-process bus, RabbitMQ and Kafka over MassTransit, several brokers in one process, transactional outbox | [Messaging](docs/messaging/overview.md) |
| **Security** | Identity (JWT, refresh token rotation, revocation, magic link, API key, OAuth, ready-made endpoints) and DataProtection (AES-GCM secret protection with key rotation) | [Identity](docs/security/identity/overview.md) |
| **Caching** | Cache abstractions with in-memory and Redis providers | [Caching](docs/caching/overview.md) |
| **Multitenancy** | Tenant resolution pipeline, tenant context, catalog and store, per-tenant execution | [Multitenancy](docs/multitenancy/overview.md) |
| **Web** | HTTP response envelopes, error mapping, response wrapping and pagination for MVC and Minimal APIs | [Web](docs/web/overview.md) |
| **User Context** | Current-user resolution from claims, typed or claims-based | [User Context](docs/user-context/overview.md) |
| **Observability** | OpenTelemetry traces, metrics and logs over OTLP, native `ILogger` or Serilog, correlation id | [Observability](docs/observability/overview.md) |
| **Communication** | E-mail (logging fake and MailKit SMTP) and the Telegram Bot API (multi-bot, long polling or webhook) | [Communication](docs/communication/overview.md) |
| **AI** | Chat completion with tool calling and multimodal input behind one contract, provider chosen per call; Gemini provider | [AI](docs/ai/overview.md) |

The full list of packages and what each one contains is in the [package matrix](docs/reference/package-matrix.md).

## Installation

The packages are published to **GitHub Packages**, not nuget.org. GitHub Packages requires authentication even to read, so you need a GitHub personal access token (classic) with the `read:packages` scope.

1. Add the feed next to nuget.org. [`nuget.config.example`](nuget.config.example) is a starting point:

   ```xml
   <packageSources>
     <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
     <add key="github" value="https://nuget.pkg.github.com/leonardopottmayer/index.json" />
   </packageSources>
   ```

2. Keep the token out of the repository: reference an environment variable instead of writing it in the file.

   ```xml
   <packageSourceCredentials>
     <github>
       <add key="Username" value="YOUR_GITHUB_USERNAME" />
       <add key="ClearTextPassword" value="%GITHUB_PACKAGES_TOKEN%" />
     </github>
   </packageSourceCredentials>
   ```

3. Reference the packages you need, all at the same version:

   ```bash
   dotnet add package Pottmayer.Tars.Communication.Telegram --version 0.0.18
   ```

## Quick start

Every family follows the same shape: bind the options, register the pieces you use, resolve the contract. Here, sending a Telegram message from a Minimal API:

```csharp
using Pottmayer.Tars.Communication.Telegram.Abstractions;
using Pottmayer.Tars.Communication.Telegram.Abstractions.Models;
using Pottmayer.Tars.Communication.Telegram.DI;

var builder = WebApplication.CreateBuilder(args);

builder.AddTarsTelegramOptions();                // binds and validates Tars:Communication:Telegram
builder.Services.AddTarsTelegramHttpClient();    // the shared HttpClient every bot uses
builder.Services.AddTarsTelegramClientFactory(); // resolves a bot by name

var app = builder.Build();

app.MapPost("/notify", async (string chatId, string text, ITelegramClientFactory telegram) =>
{
    await telegram.GetClient().SendMessageAsync(new TelegramMessage(chatId, text));
    return Results.Ok();
});

app.Run();
```

```json
{
  "Tars": {
    "Communication": {
      "Telegram": {
        "Bots": { "default": { "BotToken": "set it through an environment variable or user secrets" } }
      }
    }
  }
}
```

Each family's guide has the same walk-through for its own packages, from the minimal setup to the complete one.

## Versioning and releases

- **Lockstep versioning.** Every package is published at the same version, so a set of `Pottmayer.Tars.*` references always moves together.
- **Pre-1.0.** The `0.0.x` line can change public APIs between versions. Each release lists what changed and what to adapt under **Compatibility**.
- Release notes: [GitHub Releases](https://github.com/leonardopottmayer/tars/releases).

## Documentation

- [Documentation index](docs/README.md): every family, read by capability ("configure identity") or by reference ("which package has this contract").
- [Package taxonomy](docs/reference/taxonomy.md): the four package levels (Abstractions, Runtime, Provider, Host Integration) and the naming rules.
- [Coding conventions](docs/reference/conventions.md): `Options` vs `Configuration`, one method per registration, `TryAdd`, `AddTars*` naming, fail-fast validation.
- [Data, Multitenancy and Multi-Database](docs/data/multitenancy-and-multi-database.md): the in-depth data guide.
- [Publishing](docs/reference/publishing.md): packing and publishing the packages.

## Building and testing

```bash
dotnet build Pottmayer.Tars.slnx
```

```bash
dotnet test Pottmayer.Tars.slnx
```

Unit tests live under `tests/`, one project per family (xUnit, FluentAssertions and Moq).

## License

Pottmayer.Tars is **proprietary, not open source**. The source is public for reference and evaluation only: using, copying, modifying or distributing it, in source or binary form, requires prior written permission from the copyright holder. See [LICENSE](LICENSE).
