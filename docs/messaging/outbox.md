# Transactional In-Process Outbox

> Package: `Pottmayer.Tars.Messaging.EntityFrameworkCore`
>
> The in-process bus in [overview.md](./overview.md) publishes **after** the producer commits, and
> logs-and-swallows a failed handler. That is fire-and-forget: if the process dies between the commit
> and the publish, or a handler throws, the state is persisted but the event is gone and nothing
> retries it — a **dual-write** gap. This package closes it **without a broker**, by writing the event
> into the producer's own transaction and delivering it from a background relay.

## The idea in one paragraph

Publishing no longer delivers anything — it writes an `OutboxMessage` row in the **same transaction**
as the state change. If the transaction commits, the message is there; if it rolls back, the message
is gone with it. A background **relay** then reads pending rows and hands each event to the local
`IIntegrationEventHandler<T>` implementations, with retry, backoff and dead-lettering. Delivery is
**at-least-once**, so handlers must be idempotent on `EventId` (the same contract a broker would
impose).

This mirrors MassTransit's EF outbox already in this framework — the message lives in the application's
own DbContext because that is the only way its row can join the application's transaction — but
hand-rolled for the in-process path, reusing the broker runtime's name-to-type registry and last-mile
dispatcher.

## Does this work for every setup? (in-memory, RabbitMQ, Kafka)

Producers and consumers only ever depend on `IIntegrationEventBus` and `IIntegrationEventHandler<T>`.
That seam is what lets the **same** application and domain code run over different transports — only
the composition root changes. There are two transactional-outbox implementations, and you pick one per
deployment:

| Deployment | Transport | Outbox | Package |
|---|---|---|---|
| Modular monolith, single process | in-process (this) | **this package** | `Messaging.EntityFrameworkCore` |
| Services over a broker | RabbitMQ / Kafka | **MassTransit's EF outbox** | `Messaging.MassTransit(.RabbitMq/.Kafka).EntityFrameworkCore` |

Both honour the same rule — **publish inside the unit of work** — so the producer code is identical
either way. What differs is where the row goes and who relays it. The two flows below show this
end to end.

### Flow A — modular monolith, in-process (this package)

```
Application/Domain                         This package                     Consumer module
──────────────────                         ────────────                     ───────────────
SignUpHandler
  ExecuteAsync(identity) {
    users.Add(user)                                                        AccountActivationRequestedHandler
    bus.PublishAsync(evt) ───────────────▶ OutboxMessage row               (IIntegrationEventHandler<T>)
  }  // one SaveChanges: user + row commit together                                 ▲
                                            OutboxRelayService (bg) ────────────────┘
                                            lease due → dispatch → mark dispatched
```

1. The handler (or a domain-event translator) calls `bus.PublishAsync(evt)` **inside** `ExecuteAsync`.
   `OutboxIntegrationEventBus` writes an `OutboxMessage` into the ambient transaction.
2. `CommitAsync` runs one `SaveChanges` — the aggregate's state and the outbox row commit atomically.
3. `OutboxRelayService` (one per producing database) leases due rows, deserializes each event, and
   hands it to the local `IIntegrationEventHandler<T>` via the shared dispatcher; on success it marks
   the row dispatched, on failure it retries with backoff and eventually dead-letters.

### Flow B — services over MassTransit + RabbitMQ (or Kafka)

```
Service A (producer)                        Broker            Service B (consumer)
───────────────────                         ──────            ────────────────────
SignUpHandler
  ExecuteAsync(identity) {
    users.Add(user)
    bus.PublishAsync(evt) ──▶ MassTransit outbox row
  }  // user + row commit together
       MassTransit relay ─────────────────▶ RabbitMQ ──────▶ IntegrationEventRelayConsumer<T>
       (delivers after commit)                                 └▶ IIntegrationEventHandler<T>
```

The producer code is **the same** `bus.PublishAsync(evt)` inside the unit of work. Here the bus is
`MassTransitIntegrationEventBus`, and `UseEntityFrameworkOutbox` captures the publish into MassTransit's
outbox within the same `SaveChanges`. MassTransit's relay publishes to the broker after commit; on the
consuming service, `IntegrationEventRelayConsumer<T>` receives the message and re-dispatches it to the
local `IIntegrationEventHandler<T>` — the same handler type you wrote for the monolith. See
[brokers.md](./brokers.md) for the broker composition and the outbox call:

```csharp
services.AddTarsMassTransitRabbitMq(o =>
{
    o.Host = "localhost";
    o.Messaging.RegisterEventsFromAssembly(typeof(Events).Assembly);
    o.Messaging.RegisterHandlersFromAssembly(typeof(Handlers).Assembly);
    o.UseEntityFrameworkOutbox<TarsRabbitMqOptions, AppDbContext>(x => x.UsePostgres());
});
```

**Bottom line:** the outbox pattern works for all three (in-memory, RabbitMQ, Kafka). This package is
the in-memory implementation; the MassTransit packages are the broker one; producers and consumers move
between them unchanged.

## Producing an event: two ways

Both ways end at the same `bus.PublishAsync` → the same outbox row → the same relay. Pick per fact.

### Explicit publish

The handler publishes, **inside** the unit of work. Use it when the event's data is already at hand in
the handler (e.g. an activation token generated there). Nothing about the outbox is named in
application code — the durability is the infrastructure's business.

```csharp
await factory.ExecuteAsync(IdentityModule.DatabaseKey, async (ctx, token) =>
{
    await users.AddAsync(user, token);
    await activationTokens.AddAsync(activation, token);

    // Same IIntegrationEventBus contract — but it now writes an outbox row in THIS transaction.
    await bus.PublishAsync(new AccountActivationRequested(/* ... */), token);

    return Ok(new SignUpResult(user.Id));
}, cancellationToken: ct);
```

The one rule: publish **inside** an open unit of work. A publish with no open context has no transaction
to join, so the bus throws rather than silently recreate the dual-write. The target is the ambient
`IDataContextAccessor.Current` — the innermost active unit of work — so a publish always joins the
transaction it is written inside.

### Domain-event translation

Aggregates raise **domain events**; the command handler says nothing about publishing. An
`IDomainEventHandler<T>` **translator** turns the domain event into an integration event, and the
relational `DataContext.CommitAsync` runs those translators **before** it saves — so the outbox rows
they produce commit in the same `SaveChanges` as the aggregate. Use it when you want the command handler
pristine and the fact to originate in the domain.

```csharp
// Aggregate — ubiquitous language, no infrastructure.
public static User Register(/* ... */)
{
    var user = new User(/* ... */);
    user.Raise(new AccountRegistered(user.Id, user.Email.Value));
    return user;
}

// Command handler — no publish, no outbox.
await factory.ExecuteAsync(IdentityModule.DatabaseKey, async (ctx, token) =>
{
    var user = User.Register(/* ... */);
    await users.AddAsync(user, token);
    return Ok(new SignUpResult(user.Id));
}, cancellationToken: ct);

// Translator — domain event -> integration event, at commit, in the transaction.
public sealed class AccountRegisteredTranslator(IIntegrationEventBus bus)
    : IDomainEventHandler<AccountRegistered>
{
    public Task HandleAsync(AccountRegistered e, CancellationToken ct = default)
        => bus.PublishAsync(new AccountActivationRequested(/* ... */), ct);
}
```

A module can use both, but **one fact goes through one door** — raising *and* explicitly publishing the
same fact emits it twice (harmless with idempotent handlers, but confusing). The translation seam is
transport-agnostic: the same translator works over a broker, because it only calls `IIntegrationEventBus`.

> **Why translation reorders the commit.** `DataContext.CommitAsync` dispatches domain events *before*
> `SaveChanges`, not after. After a save the change tracker has flipped every entry to `Unchanged`, so
> collecting then would find nothing; and dispatching before is what lets a translator's outbox row ride
> the same transaction. A translator that throws aborts the commit — state and event roll back together.

## The table

A transport **envelope**, not a domain entity — deliberately no business columns. Everything specific
to the event lives in `payload` (the serialized body) and `headers` (free-form metadata). Column names
match `OutboxMessageConfiguration` exactly, and the shipped DDL (`sql/outbox.postgres.sql`) creates the
same shape. Both `payload` and `headers` are `text` (JSON content), which keeps the mapping portable
across providers.

| Column | Purpose |
|---|---|
| `id` | Row identity — a time-ordered `Guid v7`, so a plain sort drains roughly FIFO |
| `event_id` | The event's `EventId`; **unique** (a fact never enqueues twice) and the consumer dedup key |
| `event_type` | Logical name (`IntegrationEventNaming`), decoupled from the CLR type |
| `event_version` | Payload schema version — reserved for upcasting; resolved by name today |
| `payload` | The event as JSON (text) |
| `headers` | Free-form transport metadata as JSON (text), or null |
| `occurred_at` / `created_at` | When the fact happened / when the row was written |
| `status` | `0` Pending, `1` Dispatched, `2` Dead |
| `attempts` / `next_attempt_at` | Retry bookkeeping and backoff schedule |
| `processed_at` | When dispatched (drives purge retention) |
| `error` | Last failure, for diagnosis |

Extending what an event carries means changing the **event** (its payload, and `Version` if the shape
breaks) or adding a **header** — never this table.

## The relay

One background loop per producing database. Each pass drains due messages in **three phases** so a
transaction is never held open while a handler runs:

1. **Claim** — one statement locks a batch of due rows (`status = Pending AND next_attempt_at <= now`,
   oldest first) with `FOR UPDATE SKIP LOCKED` and pushes their `next_attempt_at` `LeaseDuration` into
   the future, then returns them. The lock is held only for that statement; the pushed timestamp is what
   keeps the rows invisible to other relays during delivery. `SKIP LOCKED` means a second relay instance
   simply claims a different batch — so the loop is safe to run on many instances of a scaled-out
   monolith. (This requires PostgreSQL.)
2. **Deliver** — dispatch each event through the shared last-mile dispatcher (fresh DI scope, failures propagate), recording each outcome.
3. **Record** — reopen a short transaction and stamp each row `Dispatched` or failed-with-backoff (dead-lettered once `MaxAttempts` is spent).

Keeping delivery outside the source transaction matters twice: a handler may open its own unit of work
and even publish further events (the source context is no longer ambient, so the publish target stays
unambiguous), and a slow handler never pins the producer's connection. The price is at-least-once — a
crash mid-delivery leaves the claim in place and the message reappears once the lease expires — hence
idempotent handlers. A separate purge pass deletes `Dispatched` rows older than `RetentionPeriod`.

## Registration

The pieces register one at a time — there is no "configure everything" method (the same idiom as the
broker runtime). A complete setup at the composition root:

```csharp
using Pottmayer.Tars.Messaging.Broker.DI;              // registry + last-mile dispatcher (reused)
using Pottmayer.Tars.Messaging.EntityFrameworkCore.DI; // the outbox pieces

// 1. Resolve a stored event name back to its type — scan every contracts assembly.
services.AddTarsIntegrationEventTypeRegistry(
    typeof(AccountActivationRequested).Assembly,
    typeof(NotifyUserRequested).Assembly);

// 2. The last mile: deliver to IIntegrationEventHandler<T>.
services.AddTarsIntegrationEventDispatcher();

// 3. Serialize the event body/headers.
services.AddTarsIntegrationEventSerializer();          // or AddTarsIntegrationEventSerializer<TCustom>()

// 4. Make PublishAsync write an outbox row in the producer's transaction (replaces the in-process bus).
services.AddTarsOutboxBus();

// 5. The outbox table's repository.
services.AddTarsOutboxStore();

// 6. Optional — the domain-event translation seam.
services.AddTarsOutboxDomainEventDispatcher();

// 7. One relay per producing database.
services.AddTarsOutboxRelay(IdentityModule.DatabaseKey);
services.AddTarsOutboxRelay(AgendaModule.DatabaseKey, o =>
{
    o.PollingInterval = TimeSpan.FromSeconds(2);
    o.BatchSize = 200;
    o.MaxAttempts = 8;
    o.Backoff = attempt => TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, attempt)));
    o.RetentionPeriod = TimeSpan.FromDays(7);
});

// 8. Register consumers (and, if you use step 6, the translators), once per module.
services.AddTarsIntegrationEventHandlers(typeof(SomeConsumer).Assembly);  // from Broker.DI
services.AddTarsDomainEventHandlers(typeof(SomeTranslator).Assembly);
```

Everything is idempotent (`TryAdd`) except `AddTarsOutboxBus` (removes any prior bus so it wins) and
`AddTarsOutboxRelay` (one per database), so ordering does not matter.

### Relay tuning from configuration

The relay's operational knobs — polling cadence, batch size, retry budget, lease and retention — are
the kind of thing that legitimately differs per environment, so they bind from configuration. On the
`IHostApplicationBuilder`, add:

```csharp
builder.AddTarsOutboxOptions();   // binds Tars:Messaging:Outbox
```

```jsonc
// appsettings.json
"Tars": {
  "Messaging": {
    "Outbox": {
      "PollingInterval": "00:00:05",
      "BatchSize": 100,
      "MaxAttempts": 8,
      "LeaseDuration": "00:05:00",
      "RetentionPeriod": "7.00:00:00"
    }
  }
}
```

These become the **fleet defaults** for every `AddTarsOutboxRelay`. Anything that is not per-environment
configuration stays in code: the `Backoff` function (a delegate) and any per-database override, both set
through `AddTarsOutboxRelay(key, o => ...)`, which wins over the bound defaults. `AddTarsOutboxOptions` is
optional — without it, relays use the built-in defaults. This mirrors the broker options, which bind
connection settings from configuration but keep subscriptions in code.

Map the table on each producing DbContext:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    modelBuilder.AddTarsOutbox(schema: "identity");
}
```

Create the table with the shipped DDL: copy `sql/outbox.postgres.sql`, replace `{schema}`, and run it as
a migration for each producing database.

## What changed in the core (and why it is safe)

The mechanism lives in the new package, but small additive changes were made to the framework:

- `Core.Ddd` gains `IDomainEventHandler<T>` — the translator contract.
- `IntegrationEventNameAttribute` gains `Version` (default `1`); `IntegrationEventNaming.VersionFor(...)` reads it.
- The relational `UnitOfWork.ExecuteAsync` publishes the ambient `IDataContextAccessor.Current` for the
  delegate and commit, so ambient code (the bus) can find the transaction to join.
- `DataContext.CommitAsync` now dispatches domain events **before** `SaveChanges`.

The reorder is safe because domain events had no dispatcher implementation before this package, so the
old post-save block never ran (and could not have collected anything anyway).

## Caveats

- **PostgreSQL only.** The claim uses `FOR UPDATE SKIP LOCKED`, so the relay currently requires
  PostgreSQL (it throws a clear error on any other provider). Writing and reading the row is provider
  portable; only the claim is not. Another dialect's lease can be added when a non-Postgres consumer
  appears.
- **No inbox / consumer-side dedup.** Delivery is at-least-once and dedup is the handler's job
  (idempotent on `EventId`), not a generic inbox table. That is deliberate: the relay dispatches to
  handlers that each open their own unit of work, often in different module databases, so there is no
  single handler transaction to write an inbox row into atomically. Where a specific consumer needs
  dedup, it does it locally (e.g. the notification enqueuer already skips a duplicate correlation id).
  For true cross-service exactly-once, use the broker path — MassTransit's outbox includes an inbox.
- **Multitenancy.** The relay drains a fixed database key. If a key maps to a per-tenant database, the
  relay needs to iterate the tenant catalog — not solved here. Fine for a database-per-module layout;
  an open item for database-per-tenant.
- **Upcasting.** `event_version` is stored but not yet used to transform old payloads. Bump it now so
  the column is populated; add upcasters when a contract first breaks.
```
