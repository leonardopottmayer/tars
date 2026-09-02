using MassTransit;
using Pottmayer.Tars.Messaging.Broker.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.Options;

/// <summary>
/// What every MassTransit-backed provider shares, whatever broker sits underneath.
/// </summary>
public abstract class MassTransitMessagingOptions
{
    /// <summary>What this application publishes and subscribes to. Portable across providers.</summary>
    public BrokerMessagingOptions Messaging { get; } = new();

    /// <summary>
    /// Runs inside MassTransit's registration block, where things like the outbox and sagas have to
    /// be configured. Composable with <c>+=</c>, so several extensions can each add their piece.
    /// </summary>
    /// <remarks>
    /// This is the seam the outbox uses: <c>AddEntityFrameworkOutbox</c> is an extension on
    /// <see cref="IBusRegistrationConfigurator"/>, which only exists inside that block, so it cannot
    /// be reached from the service collection afterwards.
    /// </remarks>
    public Action<IBusRegistrationConfigurator>? ConfigureRegistration { get; set; }

    /// <summary>
    /// Returns <c>true</c> when the shared options are internally consistent: the nested
    /// <see cref="Messaging"/> options pass validation. Providers override this to add their own checks,
    /// calling <c>base.IsValid()</c> first.
    /// </summary>
    public virtual bool IsValid() => Messaging is not null && Messaging.IsValid();
}
