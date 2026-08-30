-- ============================================================================
--  Tars in-process outbox — PostgreSQL DDL
-- ============================================================================
--  Creates the table behind Pottmayer.Tars.Messaging.EntityFrameworkCore's
--  transactional outbox. Column names match OutboxMessageConfiguration exactly,
--  so EF Core and this script cannot drift.
--
--  This table MUST live in the same database/schema as the state changes that
--  produce events — that is what lets a message be written in the producer's own
--  transaction and so never disagree with the state.
--
--  Usage: replace {schema} with the producing module's schema (e.g. identity,
--  channels, agenda) and run once per producing database. Feed it to migris like
--  any other migration. If you renamed the table via AddTarsOutbox(table: "..."),
--  rename it here to match.
-- ============================================================================

create table if not exists {schema}.tars_outbox_message (
    id              uuid        not null,
    event_id        uuid        not null,
    event_type      text        not null,
    event_version   integer     not null default 1,
    payload         text        not null,   -- serialized event body (JSON); text keeps the mapping provider-portable
    headers         text        null,       -- free-form metadata (JSON), or null
    occurred_at     timestamptz not null,
    created_at      timestamptz not null,
    status          smallint    not null default 0,   -- 0 = Pending, 1 = Dispatched, 2 = Dead
    attempts        integer     not null default 0,
    next_attempt_at timestamptz null,
    processed_at    timestamptz null,
    error           text        null,
    constraint pk_tars_outbox_message primary key (id)
);

-- The same fact must never enqueue twice: last-resort guard behind the producer's own idempotency.
create unique index if not exists ux_tars_outbox_message_event_id
    on {schema}.tars_outbox_message (event_id);

-- The relay's hot path: "pending, now due, oldest first". Partial, so it stays small as dispatched
-- rows accumulate before purge.
create index if not exists ix_tars_outbox_message_due
    on {schema}.tars_outbox_message (next_attempt_at, id)
    where status = 0;
