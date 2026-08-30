namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// One integration event durably parked in the producer's own database, written in the <em>same</em>
/// transaction as the state change that raised it and delivered afterwards by the relay. This is the
/// row that closes the dual-write gap: if the transaction commits, the message is here; if it rolls
/// back, the message is gone with it — the two can never disagree.
/// </summary>
/// <remarks>
/// <para>
/// The row is a transport <em>envelope</em>, not a domain entity: it deliberately carries no
/// business columns. Everything specific to the event lives in <see cref="Payload"/> (the serialized
/// body) and <see cref="Headers"/> (free-form metadata). Evolving what an event carries means
/// changing the event and, if the shape breaks, its <see cref="Version"/> — never this table.
/// </para>
/// <para>
/// Behaviour lives on the type (a small state machine) rather than in the relay so the transitions are
/// in one place and testable in isolation, mirroring the aggregates it serves alongside.
/// </para>
/// </remarks>
public sealed class OutboxMessage
{
    /// <summary>Row identity. A time-ordered <see cref="Guid.CreateVersion7()"/>, so a plain sort by id drains roughly FIFO.</summary>
    public Guid Id { get; private set; }

    /// <summary>The originating event's <c>EventId</c>. Unique — the same fact never enqueues twice — and the key consumers dedup on.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Logical event name (from <c>IntegrationEventNaming</c>), decoupled from the CLR type so a rename does not strand old rows.</summary>
    public string EventType { get; private set; } = default!;

    /// <summary>Payload schema version. Reserved for upcasting; the relay resolves the type by <see cref="EventType"/> today.</summary>
    public int Version { get; private set; }

    /// <summary>The event serialized as JSON.</summary>
    public string Payload { get; private set; } = default!;

    /// <summary>Free-form transport metadata as JSON (correlation id, tenant, trace context, ...). Null when the event carries none.</summary>
    public string? Headers { get; private set; }

    /// <summary>When the fact occurred (the event's <c>OccurredAt</c>).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>When this row was written.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Where the row is in its lifecycle.</summary>
    public OutboxMessageStatus Status { get; private set; }

    /// <summary>How many delivery attempts have been made.</summary>
    public int Attempts { get; private set; }

    /// <summary>Earliest time the relay may (re)attempt delivery. Drives backoff; null once terminal.</summary>
    public DateTimeOffset? NextAttemptAt { get; private set; }

    /// <summary>When the row reached <see cref="OutboxMessageStatus.Dispatched"/>. Drives purge retention.</summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>The last failure message, kept for diagnosis. Null while healthy.</summary>
    public string? Error { get; private set; }

    // EF materialization.
    private OutboxMessage() { }

    /// <summary>
    /// Builds a fresh <see cref="OutboxMessageStatus.Pending"/> row, immediately due. Serialization and
    /// naming are the caller's job (the bus) — this type owns only state and its transitions.
    /// </summary>
    public static OutboxMessage Enqueue(
        Guid eventId,
        string eventType,
        int version,
        string payload,
        string? headers,
        DateTimeOffset occurredAt,
        TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.GetUtcNow();
        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            EventType = eventType,
            Version = version,
            Payload = payload,
            Headers = headers,
            OccurredAt = occurredAt,
            CreatedAt = now,
            Status = OutboxMessageStatus.Pending,
            Attempts = 0,
            NextAttemptAt = now
        };
    }

    /// <summary>Marks a successful delivery. Terminal: the relay never looks at it again (until purge).</summary>
    public void MarkDispatched(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        Status = OutboxMessageStatus.Dispatched;
        ProcessedAt = clock.GetUtcNow();
        NextAttemptAt = null;
        Error = null;
    }

    /// <summary>
    /// Records a failed delivery: bumps <see cref="Attempts"/>, and either schedules a retry via
    /// <paramref name="backoff"/> or, once <paramref name="maxAttempts"/> is spent, dead-letters the row.
    /// </summary>
    public void MarkFailed(string error, int maxAttempts, Func<int, TimeSpan> backoff, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(backoff);
        ArgumentNullException.ThrowIfNull(clock);

        Attempts++;
        Error = error;

        if (Attempts >= maxAttempts)
        {
            Status = OutboxMessageStatus.Dead;
            NextAttemptAt = null;
        }
        else
        {
            NextAttemptAt = clock.GetUtcNow() + backoff(Attempts);
        }
    }
}
