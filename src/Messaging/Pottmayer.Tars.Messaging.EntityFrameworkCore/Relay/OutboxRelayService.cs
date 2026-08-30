using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pottmayer.Tars.Messaging.Broker.Dispatch;
using Pottmayer.Tars.Messaging.Broker.Registry;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Options;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Relay;

/// <summary>
/// The background loop that drains one database's outbox: it delivers due messages every
/// <see cref="OutboxDatabaseOptions.PollingInterval"/> and purges old dispatched rows every
/// <see cref="OutboxDatabaseOptions.PurgeInterval"/>. One instance is registered per producing database.
/// </summary>
/// <remarks>
/// A pass never throws out of the loop: an unexpected failure is logged and the loop waits for the next
/// tick, so a transient database blip cannot take the relay down. Delivery itself is idempotent at the
/// handler level (at-least-once), so retrying a whole pass is safe.
/// </remarks>
public sealed class OutboxRelayService : BackgroundService
{
    private readonly OutboxRelayProcessor _processor;
    private readonly OutboxDatabaseOptions _options;
    private readonly ILogger<OutboxRelayService> _logger;
    private readonly TimeProvider _timeProvider;

    public OutboxRelayService(
        IServiceScopeFactory scopeFactory,
        IIntegrationEventTypeRegistry registry,
        IIntegrationEventDispatcher dispatcher,
        IIntegrationEventSerializer serializer,
        TimeProvider timeProvider,
        ILogger<OutboxRelayService> logger,
        OutboxDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _processor = new OutboxRelayProcessor(
            scopeFactory, registry, dispatcher, serializer, timeProvider, logger, options);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox relay started for database {DatabaseKey} (polling every {PollingInterval}).",
            _options.DatabaseKey, _options.PollingInterval);

        var nextPurge = _timeProvider.GetUtcNow() + _options.PurgeInterval;

        using var timer = new PeriodicTimer(_options.PollingInterval, _timeProvider);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Drain until the queue is empty (a batch-sized pass means more may be waiting).
                int delivered;
                do
                {
                    delivered = await _processor.DrainOnceAsync(stoppingToken).ConfigureAwait(false);
                }
                while (delivered == _options.BatchSize && !stoppingToken.IsCancellationRequested);

                if (_options.PurgeEnabled && _timeProvider.GetUtcNow() >= nextPurge)
                {
                    var purged = await _processor.PurgeOnceAsync(stoppingToken).ConfigureAwait(false);
                    if (purged > 0)
                        _logger.LogDebug("Outbox purge removed {Count} dispatched rows from {DatabaseKey}.", purged, _options.DatabaseKey);
                    nextPurge = _timeProvider.GetUtcNow() + _options.PurgeInterval;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let one bad pass kill the loop; wait for the next tick and try again.
                _logger.LogError(ex, "Outbox relay pass failed for database {DatabaseKey}.", _options.DatabaseKey);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                    break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox relay stopped for database {DatabaseKey}.", _options.DatabaseKey);
    }
}
