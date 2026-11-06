# Authorization Overview

## Projects in this family

- `Pottmayer.Tars.Security.Authorization.Abstractions`
- `Pottmayer.Tars.Security.Authorization`
- `Pottmayer.Tars.Security.Authorization.AspNetCore`

## What the module offers

Permissions **per action** (`finances.cards.delete`, `finances.imports.run`), granted to a subject
through roles and through direct allow/deny exceptions. It decides "may this subject do this?";
authentication (who the subject is) stays in [Identity](../identity/overview.md), and data ownership
(which rows) stays in your handlers.

- permissions **declared in code** per module, with a CRUD shortcut, read dependencies and an optional
  feature flag per module or resource
- the declarations validated **at startup**, including every `[RequiresPermission]` in the assemblies
  you point it to
- grants as **patterns**: exact keys, prefix wildcards (`finances.*`, `finances.cards.*`) and `*`
- effective permissions = role patterns ∪ direct allows − direct denies (**deny wins**), cached per
  subject
- one check, `AuthorizationGate`: every permission's [feature flag](../../feature-flags/overview.md)
  must be on, then the subject must hold every permission
- a **mediator pipeline behavior**, so HTTP, bots and jobs dispatching requests all go through it
- a global **MVC filter** for controllers that do not dispatch through the mediator

## Declaring permissions

```csharp
using Pottmayer.Tars.Security.Authorization.DI;

builder.Services.AddTarsPermissionDefinitions(p => p
    .CheckUsagesIn(typeof(FinancesModule).Assembly)
    .Module("finances", feature: "finances")
        .Resource("cards").Crud()                                   // read, create, update, delete
        .Resource("imports", feature: "finances.import").Actions("run", "read")
        .Resource("transactions").Crud().Actions("approve")
        .Resource("statements").Read().Update());                  // single standard actions
```

- `Crud()` declares the four standard actions; `Read()`, `Create()`, `Update()` and `Delete()` declare
  one each. `Actions(...)` takes any other action.
- `PermissionActions` holds the standard action segments as constants. Build your module's keys as
  constants from them (`"finances.cards." + PermissionActions.Delete`), so the same key serves the
  declaration, `[RequiresPermission]` and any other check, with no repeated string.
- Keys are `module.resource.action`. Segments cannot be blank or contain `.` or `*`.
- Every action of a resource that declares `read` lists it in `Requires`, so a management screen can
  refuse `delete` without `read`. The check itself does not enforce dependencies.
- A resource's feature overrides the module's. A feature must be declared as a feature flag, and the
  flag catalog must be registered.
- `CheckUsagesIn` makes startup fail when a type or method in that assembly carries a
  `[RequiresPermission]` with an undeclared key.

## Registration

There is no all-in-one call. `TryAdd` is used throughout, so your own registration made **before**
these calls wins.

```csharp
builder.Services.AddTarsPermissionCatalog();        // IPermissionCatalog + startup validation
builder.Services.AddTarsPermissionChecker();        // IPermissionChecker (singleton, cached per subject)
builder.Services.AddTarsAuthorizationGate();        // feature-then-permission check
builder.Services.AddScoped<IAuthorizationStore, MyDatabaseAuthorizationStore>();

// Requests through the mediator:
builder.Services.AddTarsCqrsExceptionMappingBehavior();            // first: outermost
builder.Services.AddTarsAuthorizationBehavior();
builder.Services.AddTarsAuthorizationActorResolver<MyActorResolver>();

// Controllers that do not dispatch through the mediator (from .AspNetCore):
builder.Services.AddTarsPermissionMvcFilter();
```

Register the authorization behavior **after** the exception-mapping behavior. That behavior registers
itself with `TryAdd`, so it is skipped if any pipeline behavior was registered before it. Running it
first also makes it outermost, so a store failure is mapped too.

When a permission has a feature, `IFeatureFlags` must also be registered (see
[Feature Flags](../../feature-flags/overview.md)).

## Marking requests and controllers

```csharp
[RequiresPermission(FinancesPermissions.CardsDelete)]
public sealed class DeleteCardCommand(DeleteCardInput input) : CommandBase<DeleteCardInput, Unit>(input);
```

The behavior reads the attributes on the request type. Unmarked requests pass straight through, even
where no actor resolver is registered, so system jobs need nothing. Several attributes must all be
held.

The same attribute works on controllers and actions once the MVC filter is added. The filter reads the
subject from the `sub` claim, then `ClaimTypes.NameIdentifier`; change that with
`AddTarsPermissionMvcFilter(c => c.SubjectSelector = principal => …)`.

### Who the actor is

```csharp
public sealed class MyActorResolver : IAuthorizationActorResolver
{
    public string? Resolve(object request) => (request as IHasUserId)?.UserId.ToString();
}
```

A `null` subject on a marked request is denied.

## Outcomes

| Situation | Error type | Code |
|---|---|---|
| The permission's feature is off | `NotFound` | `feature.disabled` |
| No subject, or a permission is missing | `Forbidden` | `permission.denied` |

The behavior returns them as a failed `Result` / `Result<T>`, so the usual `IHttpErrorMapper` turns
them into 404/403 and a bot can answer with a message. The filter sends them through the same mapper.
Features are checked before permissions, so a disabled feature is off for `*` holders too.

An undeclared key used at runtime throws `InvalidOperationException`; `CheckUsagesIn` catches it at
startup instead.

## Storing grants

`IAuthorizationStore.GetGrantsAsync(subjectId)` returns `AuthorizationGrants(RolePatterns, Allowed,
Denied)`; join the subject's roles in your query. The store only **reads**. Managing roles and grants
(and auditing them) is the application's job. After a change commits:

- `IPermissionChecker.Invalidate(subjectId)` when one subject's roles or exceptions changed
- `IPermissionChecker.InvalidateAll()` when a role's patterns changed

The store is resolved from a fresh DI scope per load, so a scoped `DbContext` is fine. Patterns that
match no declared key are ignored. The cache is per process and keeps one entry per subject seen.

## Testing

Implement `IAuthorizationStore` over a dictionary, or build `AuthorizationGrants` directly and call
`AuthorizationGate.CheckAsync(subject, permissions)` to test the decision without a mediator.
