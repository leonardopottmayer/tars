# Core Overview

## What is in this family

The `Core` projects gather the most fundamental building blocks of the framework:

- `Core.Primitives`: cross-cutting utility types
- `Core.Mediator.Abstractions`: mediator contracts
- `Core.Mediator`: implementation and handler registrars
- `Core.Cqrs`: conventions for commands and queries
- `Core.Ddd`: entities, aggregates and domain events
- `Core.Localization.Abstractions`: message resolution contract (`IMessageProvider`, `IMessageSource`)
- `Core.Localization`: composition of message sources and adapters based on memory and `ResourceManager`
- `Core.Localization.AspNetCore`: integration with `IStringLocalizer` and `RequestLocalizationMiddleware`

These modules do not depend on ASP.NET Core, with the explicit exception of the `*.AspNetCore` packages, and can be used in:

- APIs
- workers
- tests
- CLI tools

## When to use

Use this family when you want to:

- standardize return values with `Result` and `Error`
- separate reads and writes with CQRS
- centralize request dispatch with a mediator
- model entities/aggregates with domain events
- support PATCH without losing the difference between "not sent" and "sent as null"
- localize framework and application messages without hardcoding

## Quick map

- [Mediator and CQRS](./mediator-cqrs.md)
- [DDD and Primitives](./ddd-primitives.md)
- [Localization](./localization.md)

## Package classification

| Package | Classification |
|---|---|
| `Core.Primitives` | Essential - base building blocks |
| `Core.Ddd` | Architectural style - optional for those who use DDD |
| `Core.Mediator.Abstractions` | Optional - needed only if the app uses a mediator |
| `Core.Mediator` | Optional - default mediator implementation |
| `Core.Cqrs` | Architectural style - optional for those who use CQRS |
| `Core.Localization.Abstractions` | Optional - message contract for framework and app |
| `Core.Localization` | Optional - message runtime based on source composition |
| `Core.Localization.AspNetCore` | Host Integration - request localization + `IStringLocalizer` |

## Minimal example

```csharp
builder.Services.AddTarsMediator(options =>
{
    options.RegisterHandlersFromAssembly(typeof(CreateOrderCommand).Assembly);
});
builder.Services.AddTarsCqrsExceptionMappingBehavior();

builder.Services.AddTarsLocalization();
builder.Services.AddTarsMessageSource(new InMemoryMessageSource(
    new Dictionary<string, IDictionary<string, string>>
    {
        ["en"] = new Dictionary<string, string>
        {
            ["app.orders.not_found"] = "Order not found."
        },
        ["pt-BR"] = new Dictionary<string, string>
        {
            ["app.orders.not_found"] = "Pedido não encontrado."
        }
    }));
```

## Important notes

- `AddTarsMediator()` registers `IMediator`, `ISender` and `IPublisher` and optionally scans handlers via `options.RegisterHandlersFromAssembly()`.
- Granular methods (`AddMediatorHandlersFromAssemblies`, `AddRequestHandlersFromAssembly`, etc.) remain available for fine-grained control.
- `CommandBase<TResult>` and `QueryBase<TResult>` already carry `CommandOptions` and `QueryOptions`.
- `Result<T>` enforces success with a non-null value and failure with at least one error.
- `AddTarsLocalization()` registers only the `IMessageProvider`. To get actual translations, add at least one source with `AddTarsMessageSource(...)` or `AddTarsStringLocalizerSource<TResource>()`.
