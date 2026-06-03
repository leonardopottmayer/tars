# Mediator and CQRS

## Mediator

### Main contracts

- `IMediator`: main interface
- `ISender`: request sending
- `IPublisher`: notification publishing
- `IRequest<TResponse>` / `IRequestHandler<TRequest, TResponse>`
- `INotification` / `INotificationHandler<TNotification>`
- `IPipelineBehavior<TRequest, TResponse>`
- `IRequestPreProcessor<TRequest>`
- `IRequestPostProcessor<TRequest, TResponse>`

### Registration

The `Pottmayer.Tars.Core.Mediator` project exposes:

```csharp
// Recommended form — registers the mediator and scans handlers in a single step
services.AddTarsMediator(options =>
{
    options.RegisterHandlersFromAssembly(typeof(MyHandler).Assembly);
});

// Multiple assemblies
services.AddTarsMediator(options =>
{
    options.RegisterHandlersFromAssembly(typeof(MyHandler).Assembly);
    options.RegisterHandlersFromAssembly(typeof(OtherHandler).Assembly);
});
```

`AddTarsMediator` registers:

- `IMediator`
- `ISender`
- `IPublisher`

And via `options.RegisterHandlersFromAssembly`, it scans and registers:

- request handlers
- notification handlers
- pipeline behaviors
- pre-processors
- post-processors

For granular control without the options pattern:

```csharp
services.AddMediatorHandlersFromAssemblies(typeof(MyHandler).Assembly);
services.AddRequestHandlersFromAssembly(assembly);
services.AddNotificationHandlersFromAssembly(assembly);
```

## CQRS

### Main types

- `ICommand<TResult>`
- `ICommandHandler<TCommand, TResult>`
- `CommandBase<TResult>`
- `CommandBase<TInput, TResult>`
- `IQuery<TResult>`
- `IQueryHandler<TQuery, TResult>`
- `QueryBase<TResult>`
- `QueryBase<TInput, TResult>`

### Per-operation options

`CommandBase` and `QueryBase` carry options on the object itself:

- `CommandOptions`
- `QueryOptions`

These options are not host configuration `Options`. They represent per-command or per-query metadata/behavior.

## Exception mapping

The `Core.Cqrs` package includes the `ExceptionMappingBehavior<TRequest, TResponse>` behavior.

Default registration:

```csharp
services.AddTarsCqrsExceptionMappingBehavior();
```

With a custom mapper:

```csharp
services.AddTarsCqrsExceptionMappingBehavior(ex =>
{
    if (ex is DomainException dex)
        return new[] { new Error("domain_error", dex.Message, ErrorType.Validation) };

    return Array.Empty<Error>();
});
```

This behavior is useful when your handlers return `Result` and you want to turn expected exceptions into domain errors consistently.

## Recommended pattern

```csharp
builder.Services.AddTarsMediator(options =>
{
    options.RegisterHandlersFromAssembly(typeof(MyApplicationAssemblyMarker).Assembly);
});
builder.Services.AddTarsCqrsExceptionMappingBehavior();
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

## Best practices

- use commands for state changes
- use queries for reads
- keep handlers small and coordinating dependencies
- prefer behaviors for cross-cutting concerns such as validation, logging and exception mapping
