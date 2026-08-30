using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// EF Core mapping for <see cref="OutboxMessage"/>. Apply it on the producing module's DbContext — the
/// table has to live in the application's own context, because that is the only way its rows join the
/// application's transaction. Mirrors the deliberate "your DbContext hosts the outbox" arrangement the
/// MassTransit provider already uses.
/// </summary>
/// <param name="schema">Schema to place the table in, or <c>null</c> for the provider default.</param>
/// <param name="table">Table name, or <c>null</c> for <see cref="OutboxStorage.DefaultTable"/>.</param>
public sealed class OutboxMessageConfiguration(string? schema = null, string? table = null)
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(table ?? OutboxStorage.DefaultTable, schema);

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName(OutboxStorage.IdColumn).ValueGeneratedNever();

        builder.Property(m => m.EventId).HasColumnName(OutboxStorage.EventIdColumn);
        // The same fact must never enqueue twice: the unique index is the last-resort guard behind the
        // producer's own idempotency, enforced by the database rather than trusted from the caller.
        builder.HasIndex(m => m.EventId).IsUnique();

        builder.Property(m => m.EventType).HasColumnName(OutboxStorage.EventTypeColumn).IsRequired();
        builder.Property(m => m.Version).HasColumnName(OutboxStorage.VersionColumn);
        builder.Property(m => m.Payload).HasColumnName(OutboxStorage.PayloadColumn).IsRequired();
        builder.Property(m => m.Headers).HasColumnName(OutboxStorage.HeadersColumn);
        builder.Property(m => m.OccurredAt).HasColumnName(OutboxStorage.OccurredAtColumn);
        builder.Property(m => m.CreatedAt).HasColumnName(OutboxStorage.CreatedAtColumn);
        builder.Property(m => m.Status).HasColumnName(OutboxStorage.StatusColumn).HasConversion<short>();
        builder.Property(m => m.Attempts).HasColumnName(OutboxStorage.AttemptsColumn);
        builder.Property(m => m.NextAttemptAt).HasColumnName(OutboxStorage.NextAttemptAtColumn);
        builder.Property(m => m.ProcessedAt).HasColumnName(OutboxStorage.ProcessedAtColumn);
        builder.Property(m => m.Error).HasColumnName(OutboxStorage.ErrorColumn);

        // The relay's hot path: "pending, now due, oldest first". Filtered to pending so the index stays
        // small as dispatched rows pile up before purge.
        builder.HasIndex(m => new { m.NextAttemptAt, m.Id })
            .HasDatabaseName("ix_" + (table ?? OutboxStorage.DefaultTable) + "_due")
            .HasFilter(null);
    }
}
