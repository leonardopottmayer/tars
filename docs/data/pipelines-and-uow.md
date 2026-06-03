# Data — Contracts, Pipelines and Unit of Work

The core contracts (`IUnitOfWork`, `IDataContext`, `IRepository`, `QueryParams`) live in `Pottmayer.Tars.Data.Abstractions` and are provider-agnostic. The application layer only needs to reference this package — it does not need to know the data backend.

---

## IUnitOfWorkFactory and IUnitOfWork

`IUnitOfWorkFactory` is injected into handlers and services. It is the main entry point for data access.

```csharp
public interface IUnitOfWorkFactory
{
    // Creates an explicit UnitOfWork — useful for multiple operations in the same UoW
    IUnitOfWork Create(string databaseKey);
    IUnitOfWork Create();                   // uses DataKeys.Default

    // Shortcut: creates, runs, commits and disposes automatically
    Task ExecuteAsync(
        string databaseKey,
        Func<IDataContext, CancellationToken, Task> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteAsync<T>(
        string databaseKey,
        Func<IDataContext, CancellationToken, Task<T>> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default);

    // Keyless overloads — equivalent to the above with DataKeys.Default
    Task ExecuteAsync(
        Func<IDataContext, CancellationToken, Task> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteAsync<T>(
        Func<IDataContext, CancellationToken, Task<T>> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IAsyncDisposable
{
    ValueTask<IDataContext> GetContextAsync(CancellationToken ct = default); // context created lazily on the first call
    Task CommitAsync(CancellationToken ct = default);
    Task ExecuteAsync(Func<IDataContext, CancellationToken, Task> work, UnitOfWorkOptions? options = null, CancellationToken ct = default);
    Task<T> ExecuteAsync<T>(Func<IDataContext, CancellationToken, Task<T>> work, UnitOfWorkOptions? options = null, CancellationToken ct = default);
}
```

### Form 1 — factory.ExecuteAsync (recommended)

Ideal for most cases: one unit of work, auto-commit, auto-dispose.

Single-database apps omit the key — the framework uses `DataKeys.Default` automatically:

```csharp
public sealed class CreateOrderHandler(IUnitOfWorkFactory factory)
    : ICommandHandler<CreateOrderCommand>
{
    public async Task<Result> HandleAsync(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = await factory.ExecuteAsync(async (ctx, token) =>
        {
            var repo = ctx.AcquireRepository<IOrderRepository>();
            var order = Order.Create(cmd.CustomerId, cmd.Items);
            await repo.AddAsync(order, token);
            return order;
        }, cancellationToken: ct);

        return Result.Ok(order.Id);
    }
}
```

When there are multiple databases, pass the key explicitly:

```csharp
var centralData = await factory.ExecuteAsync("central", async (ctx, token) => ..., cancellationToken: ct);
await factory.ExecuteAsync(async (ctx, token) => ..., cancellationToken: ct); // default database
```

### Form 2 — Create + uow.ExecuteAsync (multiple operations in the same UoW)

When you need more than one work block sharing the same context and transaction:

```csharp
public async Task<Result> HandleAsync(TransferCommand cmd, CancellationToken ct)
{
    await using var uow = _factory.Create(); // DataKeys.Default

    var source = await uow.ExecuteAsync(async (ctx, token) =>
        await ctx.AcquireRepository<IAccountRepository>().GetByIdAsync(cmd.SourceId, token),
        options: new UnitOfWorkOptions { CommitOnSuccess = false },
        ct: ct);

    if (source is null || source.Balance < cmd.Amount)
        return Result.Fail("Insufficient funds.");

    await uow.ExecuteAsync(async (ctx, token) =>
    {
        var repo = ctx.AcquireRepository<IAccountRepository>();
        source.Debit(cmd.Amount);
        await repo.UpdateAsync(source, token);
    }, ct: ct);

    return Result.Ok();
}
```

### Form 3 — Create + GetContextAsync (full control, conditional flows)

When the decision to commit depends on business logic:

```csharp
public async Task<Result> HandleAsync(ShipOrderCommand cmd, CancellationToken ct)
{
    await using var uow = _factory.Create(); // DataKeys.Default

    var ctx  = await uow.GetContextAsync(ct);
    var repo = ctx.AcquireRepository<IOrderRepository>();
    var order = await repo.GetByIdAsync(cmd.OrderId, ct);

    if (order is null)
        return Result.Fail("Order not found.");

    if (!order.CanShip())
        return Result.Fail("Order cannot be shipped in current state.");

    order.Ship();
    await uow.CommitAsync(ct);

    return Result.Ok();
}
```

### Disabling auto-commit

Applies to forms 1 and 2. Useful for read-only operations:

```csharp
var result = await factory.ExecuteAsync(async (ctx, ct) =>
{
    return await ctx.AcquireRepository<IOrderRepository>().GetByIdAsync(id, ct);
}, options: new UnitOfWorkOptions { CommitOnSuccess = false }, cancellationToken: ct);
```

---

## IDataContext

```csharp
public interface IDataContext : IAsyncDisposable
{
    IRepositoryResolver Resolver { get; }
    TRepository AcquireRepository<TRepository>() where TRepository : class, IRepository;
    Task CommitAsync(CancellationToken cancellationToken = default);
    void CollectDomainEvents(IHasDomainEvents aggregate);
}
```

`AcquireRepository<T>()` resolves the repository via DI, temporarily setting the ambient context so the repository knows which context it belongs to.

---

## Nested contexts — transaction shared across handlers

When a handler calls another handler internally (e.g. via the mediator or direct injection), the second handler can participate in the **same transaction** as the first without any extra configuration.

The mechanism is automatic: `CreateScopedAsync` (used internally by `UnitOfWork`) checks whether there is already an active context for the same `databaseKey` in the current async scope. If there is, it returns a borrowed context that shares the same `DbContext` and connection.

### How it works

```csharp
// HandlerA — opens the UoW normally
public async Task HandleAsync(CommandA cmd, CancellationToken ct)
{
    await _factory.ExecuteAsync(async (ctx, token) =>
    {
        var repo = ctx.AcquireRepository<IOrderRepository>();
        var order = Order.Create(cmd.CustomerId);
        await repo.AddAsync(order, token);

        // Calls HandlerB — which also uses uowFactory.ExecuteAsync internally
        await _handlerB.HandleAsync(new CommandB(order.Id), token);

        // If HandlerB threw, it propagated up to here → no commit happens → full rollback
        // If everything is ok → this CommitAsync persists the changes of A and B in a single SaveChangesAsync
    }, cancellationToken: ct);
}

// HandlerB — coded normally, with no awareness that it may be nested
public async Task HandleAsync(CommandB cmd, CancellationToken ct)
{
    await _factory.ExecuteAsync(async (ctx, token) =>
    {
        var repo = ctx.AcquireRepository<IInventoryRepository>();
        var inv = await repo.GetByOrderIdAsync(cmd.OrderId, token);
        inv!.Reserve();
        await repo.UpdateAsync(inv, token);
        // CommitAsync here is a no-op — the real transaction belongs to HandlerA
    }, cancellationToken: ct);
}
```

### Guarantees

| Scenario | Result |
|---|---|
| HandlerB fails | Exception propagates to HandlerA → HandlerA does not commit → full rollback |
| HandlerA fails after HandlerB | HandlerA does not commit → everything HandlerB did is also discarded |
| Both ok | HandlerA commits — `SaveChangesAsync` encompasses both sets of changes in a single transaction |
| HandlerB calls `CommitAsync` explicitly | Silent no-op — only the outer handler commits |

### Domain events in nested contexts

Domain events collected in HandlerB (via `ctx.CollectDomainEvents()` or automatically by the change tracker) accumulate in the same `DataContext`. They are all dispatched together in HandlerA's `CommitAsync`. This is the correct behavior for transactional consistency — the events do not fire before the commit.

### CreateIsolatedAsync — a truly isolated context

`CreateIsolatedAsync` never participates in the ambient: it always creates a new `DbContext`, even if an active context already exists. Use it when the operation needs a separate transaction independent of the outer context.

```csharp
// This context does not share a transaction with any outer context
var ctx = await _contextFactory.CreateIsolatedAsync(DataKeys.Default, ct);
```

---

## IDataContextFactory (relational)

Rarely used directly (prefer `IUnitOfWorkFactory`). It exposes two methods:

- **`CreateScopedAsync`** — participates in the ambient: returns the existing context for the same key if one is active in the current scope (see the section above). Default `UnitOfWork` behavior.
- **`CreateIsolatedAsync`** — always creates a new independent context, even inside a nested scope.

```csharp
// IDataContextFactory
var ctx = await _contextFactory.CreateScopedAsync(DataKeys.Default, ct);   // join the ambient if it exists
var ctx = await _contextFactory.CreateIsolatedAsync(DataKeys.Default, ct); // always new
```

---

## IStandardRepository (relational)

Standard CRUD interface for the relational axis — all methods are async:

```csharp
public interface IStandardRepository<TEntity, TKey> : IRepository<TEntity>
{
    // Queryable — EF Core only, do not use in provider-agnostic code
    IQueryable<TEntity> Queryable(Expression<Func<TEntity, bool>>? predicate = null);

    Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    Task<TEntity?> RemoveByKeyAsync(TKey key, CancellationToken ct = default);
    Task<TEntity> RemoveAsync(TEntity entity, CancellationToken ct = default);
    Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    Task<bool> ExistsKeyAsync(TKey key, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    Task<IEnumerable<TEntity>> GetPagedAsync(int skip, int take, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    Task<DataQueryResult<TEntity>> ExecuteQueryAsync(QueryParams? queryParams = null, CancellationToken ct = default);
}
```

---

## RepositoryBase and StandardRepository (relational)

### Simple repository — EF Core only

```csharp
// Interface (domain — zero dependency on Tars or any provider)
public interface IOrderRepository : IStandardRepository<Order, Guid>
{
    Task<Order?> FindByNumberAsync(string number, CancellationToken ct);
}

// Implementation (infrastructure — references Tars.Data.Relational)
public sealed class OrderRepository : StandardRepository<Order, Guid>, IOrderRepository
{
    protected override IReadOnlySet<string> AllowedQueryFields { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Status", "CustomerId", "CreatedAt" };

    public OrderRepository(IDataContextAccessor accessor) : base(accessor) { }

    public Task<Order?> FindByNumberAsync(string number, CancellationToken ct)
        => DbContext.Set<Order>().FirstOrDefaultAsync(o => o.Number == number, ct);
}
```

### Repository with Dapper for analytical reads

EF and Dapper share the same connection and transaction:

```csharp
public sealed class OrderRepository : StandardRepository<Order, Guid>, IOrderRepository
{
    public OrderRepository(IDataContextAccessor accessor) : base(accessor) { }

    protected override IReadOnlySet<string> AllowedQueryFields { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Status", "CustomerId" };

    public async Task<IReadOnlyList<OrderSummaryDto>> GetSummariesAsync(
        DateTimeOffset from, CancellationToken ct)
    {
        return (await Connection.QueryAsync<OrderSummaryDto>(
            @"SELECT o.id, o.status, COUNT(i.id) AS item_count
              FROM orders o
              JOIN order_items i ON i.order_id = o.id
              WHERE o.created_at >= @from
              GROUP BY o.id, o.status",
            new { from })).AsList();
    }
}
```

### Custom repository (without StandardRepository)

```csharp
public interface IReportingRepository : IRepository
{
    Task<IReadOnlyList<SalesReportRow>> GetSalesReportAsync(DateRange range, CancellationToken ct);
}

public sealed class ReportingRepository : RepositoryBase, IReportingRepository
{
    public ReportingRepository(IDataContextAccessor accessor) : base(accessor) { }

    public async Task<IReadOnlyList<SalesReportRow>> GetSalesReportAsync(DateRange range, CancellationToken ct)
    {
        return (await Connection.QueryAsync<SalesReportRow>(
            @"SELECT DATE_TRUNC('day', created_at) AS day, SUM(total) AS total
              FROM orders
              WHERE created_at BETWEEN @start AND @end
              GROUP BY 1 ORDER BY 1",
            new { start = range.Start, end = range.End })).AsList();
    }
}
```

---

## RelationalDbContext — the application's DbContext

```csharp
public sealed class AppDbContext : RelationalDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

---

## QueryParams and ExecuteQueryAsync

`ExecuteQueryAsync` enables dynamic filtering, sorting and pagination. It works across both providers. Only fields present in `AllowedQueryFields` are accepted — others are silently ignored.

### Building QueryParams

```csharp
var query = new QueryParams()
    .AddFilter("Status", FilterOperator.Eq, "Active")
    .AddFilter("CreatedAt", FilterOperator.Gte, DateTime.UtcNow.AddDays(-30))
    .SetOrderBy("CreatedAt", descending: true)
    .SetPaged(page: 1, pageSize: 20);

var result = await repo.ExecuteQueryAsync(query, ct);
// result.Items — list of entities/documents
// result.TotalCount — total before pagination (long)
```

### Available operators

| Operator | Relational |
|---|---|
| `Eq` | `=` |
| `NotEq` | `<>` |
| `Contains` | `LIKE %value%` |
| `StartsWith` | `LIKE value%` |
| `EndsWith` | `LIKE %value` |
| `Gt` | `>` |
| `Gte` | `>=` |
| `Lt` | `<` |
| `Lte` | `<=` |
| `In` | `IN (...)` |

### `In` filter

```csharp
var query = new QueryParams()
    .AddFilterIn("Status", ["Active", "Pending"]);
```

### Multiple orderings

```csharp
var query = new QueryParams()
    .SetOrderBy("Category")     // primary
    .AddOrderBy("Name");        // secondary
```

---

## Domain Events

### Automatic flow (relational only — EF change tracker)

```csharp
public sealed class Order : AggregateRoot
{
    public void Ship()
    {
        Status = OrderStatus.Shipped;
        AddDomainEvent(new OrderShippedEvent(Id));
    }
}

// In the handler — the event is collected automatically on CommitAsync via the change tracker
order.Ship();
await uow.CommitAsync(ct);
```

### Manual collection (Dapper / raw SQL, outside the change tracker)

```csharp
await uow.ExecuteAsync(async (ctx, ct) =>
{
    var repo = ctx.AcquireRepository<IOrderRepository>();

    // Modification outside tracking (Dapper or raw SQL)
    await repo.UpdateViaRawOperationAsync(cmd.OrderId, ct);

    // The aggregate must be loaded to emit the event
    var order = await repo.GetByIdAsync(cmd.OrderId, ct);
    order!.RecordShipment();    // internal AddDomainEvent

    // Manual collection — no change tracker to detect the change
    ctx.CollectDomainEvents(order);
}, ct: ct);
```

---

## Multi-database in the same handler

Each `factory.ExecuteAsync` creates an independent `UnitOfWork`. They can coexist in the same handler:

```csharp
public async Task HandleAsync(SyncCommand cmd, CancellationToken ct)
{
    var centralData = await _factory.ExecuteAsync("central", async (ctx, token) =>
        await ctx.AcquireRepository<ICentralRepository>().GetAllAsync(token),
        options: new UnitOfWorkOptions { CommitOnSuccess = false },
        cancellationToken: ct);

    await _factory.ExecuteAsync(DataKeys.Default, async (ctx, token) =>
    {
        var repo = ctx.AcquireRepository<ITenantRepository>();
        foreach (var item in centralData)
            await repo.AddAsync(TenantEntity.From(item), token);
    }, cancellationToken: ct);
}
```

---

## IMultiDatabaseCoordinator (relational)

Coordination of multiple relational databases with compensation on failure:

```csharp
await _coordinator.ExecuteAsync(
    databaseKeys: ["default", "central"],
    work: async (ctx, ct) =>
    {
        var orderUow   = ctx.GetUnitOfWork("default");
        var centralUow = ctx.GetUnitOfWork("central");

        var orderCtx   = await orderUow.GetContextAsync(ct);
        var centralCtx = await centralUow.GetContextAsync(ct);

        await orderCtx.AcquireRepository<IOrderRepository>()
            .AddAsync(order, ct);

        await centralCtx.AcquireRepository<ICentralOrderRepository>()
            .SyncAsync(order.ToSyncDto(), ct);
    },
    compensate: async (ctx, ex, ct) =>
    {
        var compensateCtx = await ctx.GetUnitOfWork("central").GetContextAsync(ct);
        var repo = compensateCtx.AcquireRepository<ICentralOrderRepository>();
        await repo.RollbackSyncAsync(order.Id, ct);
    },
    cancellationToken: ct);
```

> **Important:** `IMultiDatabaseCoordinator` does not guarantee distributed atomicity (it is not 2PC).
> It is a best-effort sequential commit. For eventual consistency in production, use the Outbox pattern.

---

## Repository lifecycle

Repositories **must be Transient**. They capture the `IDataContext` in the constructor, at the exact moment they are resolved via DI — while `AcquireRepository<T>()` keeps the context accessible synchronously. Registering them as Scoped or Singleton would cause the context to be reused across requests.

`AddTarsDataRepositoriesFromAssemblies` registers them as Transient by default.
