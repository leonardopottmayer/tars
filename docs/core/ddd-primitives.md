# DDD and Primitives

## DDD

### Main types

- `Entity<TKey>`: base entity with identity
- `AggregateRoot<TKey>`: aggregate root
- `IDomainEvent`: domain event contract
- `IHasDomainEvents`: exposes pending events
- `IDomainEventDispatcher`: dispatcher implemented by the consumer

### Example

```csharp
public sealed class Order : AggregateRoot<Guid>
{
    public string Number { get; private set; }

    public Order(Guid id, string number) : base(id)
    {
        Number = number;
    }
}
```

The framework does not impose a complete domain event bus on its own. It defines the foundation so that your data adapter dispatches the events at the appropriate moment.

## Result and Error

### `Result`

`Result` and `Result<T>` are the standard mechanism for representing success and failure:

- success: `IsSuccess = true`, no errors
- failure: `IsSuccess = false`, with at least one error

Example:

```csharp
return Result<UserDto>.Success(user);
return Result<UserDto>.Failure(new Error("user_not_found", "User not found", ErrorType.NotFound));
```

### `Error`

Use `Error` to carry:

- code
- message
- semantic type (`Validation`, `Conflict`, `NotFound`, etc.)

This integrates well with `Web.Http`, which can map the failure to an HTTP status and a consistent envelope.

## `Optional<T>`

`Optional<T>` exists for PATCH scenarios, where you need to distinguish:

- property absent from the JSON
- property present with a `null` value
- property present with a defined value

Example:

```csharp
public sealed class PatchUserRequest
{
    public Optional<string?> DisplayName { get; init; }
}
```

When used with `OptionalJsonConverterFactory`, an absent property becomes `Optional<T>.Absent()`.

## When to use `Optional<T>`

- partial PATCH of REST APIs
- granular update commands
- normalization where `null` has different semantics from "not provided"

## Note

The example apps use `OptionalJsonConverterFactory` in the HTTP adapter, not in the domain. This is usually the best separation: transport interprets the partial update, the application decides how to apply it.
