using MassTransit;
using Microsoft.EntityFrameworkCore;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.MassTransit.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.EntityFrameworkCore;

/// <summary>
/// Turns on MassTransit's transactional outbox for any Tars MassTransit provider — RabbitMQ, Kafka,
/// or whichever comes next.
/// </summary>
/// <remarks>
/// <para>
/// The outbox is MassTransit's, not a third implementation. Publishing after a commit is a second
/// commit that can fail on its own, and the event then vanishes with nobody noticing; the outbox
/// writes the message in the <em>same transaction</em> as the state change and a relay delivers it
/// afterwards. That machinery already exists in the framework we depend on, so wrapping ours around
/// it would be the abstraction-over-abstraction this layer exists to avoid.
/// </para>
/// <para>
/// Producers keep calling <see cref="IIntegrationEventBus.PublishAsync"/> and cannot tell the
/// difference. What changes is only when the message leaves.
/// </para>
/// </remarks>
public static class MassTransitEntityFrameworkCoreOutboxExtensions
{
    /// <summary>
    /// Routes publishing through the outbox stored in <typeparamref name="TDbContext"/>.
    /// </summary>
    /// <typeparam name="TOptions">The provider options type, extending <see cref="MassTransitMessagingOptions"/>.</typeparam>
    /// <typeparam name="TDbContext">The EF Core database context type hosting the outbox tables.</typeparam>
    /// <param name="options">The provider options instance (e.g., <c>MassTransitRabbitMqMessagingOptions</c>).</param>
    /// <param name="configure">
    /// The database dialect and delivery settings. At minimum pick a dialect —
    /// <c>o.UsePostgres()</c>, <c>o.UseSqlServer()</c> — because the outbox takes row locks and the
    /// SQL for that is not portable.
    /// </param>
    /// <returns>The configured options instance for method chaining.</returns>
    /// <remarks>
    /// <typeparamref name="TDbContext"/> must include MassTransit's outbox entity configurations
    /// (<c>InboxState</c>, <c>OutboxMessage</c>, <c>OutboxState</c>) and a migration creating them.
    /// That is a deliberate part of the design, not an oversight: the whole point is that those rows
    /// are written by the application's own transaction, so they have to live in the application's
    /// own context.
    /// </remarks>
    public static TOptions UseEntityFrameworkOutbox<TOptions, TDbContext>(
        this TOptions options,
        Action<IEntityFrameworkOutboxConfigurator> configure)
        where TOptions : MassTransitMessagingOptions
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);

        // Composed with += so this can sit alongside other registration-time extensions.
        options.ConfigureRegistration += bus => bus.AddEntityFrameworkOutbox<TDbContext>(outbox =>
        {
            configure(outbox);

            // Without this the outbox stores nothing on publish, which is the one mistake that makes
            // the feature look configured while doing nothing at all.
            outbox.UseBusOutbox();
        });

        return options;
    }
}
