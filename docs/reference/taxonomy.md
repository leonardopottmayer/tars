# Package Taxonomy

Every Tars package belongs to one of the four levels below. The level is explicit in the project name — not in intermediate folders.

## The Four Levels

| Level | What it contains | Depends on |
|---|---|---|
| **Abstractions** | Interfaces, contracts, base types. No implementation. | Nothing (or only other `.Abstractions`) |
| **Runtime** | Default implementation of the contracts. Can be replaced. | `.Abstractions` of the same group |
| **Provider / Adapter** | Implementation tied to a specific technology (EFCore, Redis, MongoDB…). | `.Abstractions` of the group |
| **Host Integration** | Adapter for a specific host (AspNetCore, Worker, Console). Thin and optional. | `.Abstractions` + the host SDK |

### Naming rule

```
Pottmayer.Tars.<Group>.<Subgroup?>.Abstractions   → Abstractions
Pottmayer.Tars.<Group>.<Subgroup?>                → Runtime
Pottmayer.Tars.<Group>.<Subgroup?>.<Technology>   → Provider / Adapter
Pottmayer.Tars.<Group>.<Subgroup?>.AspNetCore      → Host Integration
```

## Quality Criteria for an Abstraction

- Works without ASP.NET Core
- Has useful semantics in any .NET host (console, worker, Blazor, web)
- Does not carry provider details
- Does not leak the topology of a specific solution
- If it only makes sense in a single host → it belongs to that host's adapter, not the core

## Criteria for Creating a New Package

Create a new NuGet only when there is at least one of these justifications:

- A clear, cohesive responsibility
- The possibility of isolated reuse
- Its own evolution cycle
- A well-defined technology or provider
- A real composition gain for the consumer

## Absolute Rule: Folder = Namespace

Every folder segment in the physical path must appear in the namespace of the project inside it.

| Physical path | Project namespace | Valid? |
|---|---|---|
| `src/Data/Relational/Pottmayer.Tars.Data.Relational.EFCore/` | `Pottmayer.Tars.Data.Relational.EFCore` | ✓ |
| `src/Security/Identity/Pottmayer.Tars.Security.Identity.AspNetCore/` | `Pottmayer.Tars.Security.Identity.AspNetCore` | ✓ |
| `src/Caching/Runtime/Pottmayer.Tars.Caching.Core/` | `Pottmayer.Tars.Caching.Core` | ✗ `Runtime` is not in the namespace |
| `src/Web/Http/Hosts/Pottmayer.Tars.Web.Http.AspNetCore/` | `Pottmayer.Tars.Web.Http.AspNetCore` | ✗ `Hosts` is not in the namespace |

The distinction between levels lives **in the project name**, not in the parent folder.

## Classification by Usage

Besides the architectural level, each package has a usage classification:

| Classification | Meaning |
|---|---|
| **Essential** | Base dependency of any application using the framework |
| **Architectural style** | Optional; relevant only if the app adopts a given pattern (DDD, CQRS…) |
| **Optional** | Needed only if the corresponding feature is used |
| **Support infrastructure** | Internal framework utility; rarely referenced directly |
