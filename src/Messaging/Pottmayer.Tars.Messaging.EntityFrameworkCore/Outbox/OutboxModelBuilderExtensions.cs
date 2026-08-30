using Microsoft.EntityFrameworkCore;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// Adds the outbox table to a producing module's DbContext.
/// </summary>
public static class OutboxModelBuilderExtensions
{
    /// <summary>
    /// Maps <see cref="OutboxMessage"/> onto <paramref name="modelBuilder"/>. Call it from the
    /// DbContext's <c>OnModelCreating</c> so the outbox lives in the same context — and therefore the
    /// same transaction — as the state changes that produce events.
    /// </summary>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    ///     modelBuilder.AddTarsOutbox(schema: "identity");
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder AddTarsOutbox(this ModelBuilder modelBuilder, string? schema = null, string? table = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(schema, table));
        return modelBuilder;
    }
}
