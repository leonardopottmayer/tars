using FluentAssertions;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker;
using Pottmayer.Tars.Messaging.Broker.Options;
using Pottmayer.Tars.Messaging.Broker.Registry;
using Pottmayer.Tars.Messaging.Broker.Routing;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

// Fixtures: one broadcast event, one routed, one headered, one unnamed.

[IntegrationEventName("identity.password-reset.v1")]
public sealed record PasswordResetRequested(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;

[IntegrationEventName("inbound.interaction")]
public sealed record InboundInteractionReceived(
    Guid EventId, DateTimeOffset OccurredAt, string OwnerModule, string Action)
    : IIntegrationEvent, IRoutedIntegrationEvent
{
    public string RoutingKeySuffix => $"{OwnerModule}.{Action}";
}

[IntegrationEventName("billing.invoice-issued.v1")]
public sealed record InvoiceIssued(Guid EventId, DateTimeOffset OccurredAt, string TenantId)
    : IIntegrationEvent, IHeaderedIntegrationEvent
{
    public IReadOnlyDictionary<string, string> Headers => new Dictionary<string, string>
    {
        ["tars.tenant-id"] = TenantId,
    };
}

public sealed record MfaEnabled(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record BadlyRouted(Guid EventId, DateTimeOffset OccurredAt, string Suffix)
    : IIntegrationEvent, IRoutedIntegrationEvent
{
    public string RoutingKeySuffix => Suffix;
}

public class IntegrationEventNamingTests
{
    [Fact]
    public void For_prefers_the_explicit_attribute()
        => IntegrationEventNaming.For<PasswordResetRequested>().Should().Be("identity.password-reset.v1");

    [Fact]
    public void For_falls_back_to_kebab_case_of_the_type_name()
        => IntegrationEventNaming.For<MfaEnabled>().Should().Be("mfa-enabled");

    [Theory]
    [InlineData(typeof(PasswordResetRequested), true)]
    [InlineData(typeof(MfaEnabled), false)]
    public void IsExplicit_reports_whether_the_name_was_declared(Type type, bool expected)
        => IntegrationEventNaming.IsExplicit(type).Should().Be(expected);
}

public class DefaultIntegrationEventRouterTests
{
    private static readonly DefaultIntegrationEventRouter Router = new();

    [Fact]
    public void An_event_without_the_routing_interface_is_broadcast()
    {
        var route = Router.Resolve(new PasswordResetRequested(Guid.NewGuid(), DateTimeOffset.UtcNow));

        route.Destination.Should().Be("identity.password-reset.v1");
        route.RoutingKey.Should().BeNull();
        route.IsBroadcast.Should().BeTrue();
    }

    [Fact]
    public void A_routed_event_gets_its_suffix_appended_to_the_event_name()
    {
        var route = Router.Resolve(
            new InboundInteractionReceived(Guid.NewGuid(), DateTimeOffset.UtcNow, "agenda", "task_done"));

        route.Destination.Should().Be("inbound.interaction");
        route.RoutingKey.Should().Be("inbound.interaction.agenda.task_done");
        route.IsBroadcast.Should().BeFalse();
    }

    [Fact]
    public void Headers_travel_when_the_event_declares_them()
    {
        var route = Router.Resolve(new InvoiceIssued(Guid.NewGuid(), DateTimeOffset.UtcNow, "acme"));

        route.Headers.Should().Contain(new KeyValuePair<string, string>("tars.tenant-id", "acme"));
    }

    [Fact]
    public void An_event_without_headers_carries_an_empty_collection_rather_than_null()
        => Router.Resolve(new MfaEnabled(Guid.NewGuid(), DateTimeOffset.UtcNow)).Headers.Should().BeEmpty();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_routing_suffix_fails_loudly(string suffix)
    {
        var act = () => Router.Resolve(new BadlyRouted(Guid.NewGuid(), DateTimeOffset.UtcNow, suffix));

        act.Should().Throw<InvalidOperationException>().WithMessage("*empty routing key suffix*");
    }

    [Theory]
    [InlineData("agenda.#")]
    [InlineData("agenda.*")]
    public void A_wildcard_in_a_published_key_fails_loudly(string suffix)
    {
        var act = () => Router.Resolve(new BadlyRouted(Guid.NewGuid(), DateTimeOffset.UtcNow, suffix));

        act.Should().Throw<InvalidOperationException>().WithMessage("*wildcard*subscription pattern*");
    }
}

public class IntegrationEventSubscriptionTests
{
    [Fact]
    public void Broadcast_binds_the_whole_destination()
    {
        var subscription = IntegrationEventSubscription.Broadcast<PasswordResetRequested>();

        subscription.Destination.Should().Be("identity.password-reset.v1");
        subscription.RoutingKeyPattern.Should().BeNull();
    }

    [Fact]
    public void Matching_makes_the_pattern_absolute_against_the_event_name()
    {
        var subscription = IntegrationEventSubscription.Matching<InboundInteractionReceived>("agenda.#");

        subscription.Destination.Should().Be("inbound.interaction");
        subscription.RoutingKeyPattern.Should().Be("inbound.interaction.agenda.#");
    }

    [Fact]
    public void A_published_key_matches_only_the_owning_module_pattern()
    {
        // The whole point of the design: Agenda's binding must not catch Assistant's interactions.
        var published = new DefaultIntegrationEventRouter()
            .Resolve(new InboundInteractionReceived(Guid.NewGuid(), DateTimeOffset.UtcNow, "agenda", "task_done"))
            .RoutingKey!;

        var agenda = IntegrationEventSubscription.Matching<InboundInteractionReceived>("agenda.#");
        var assistant = IntegrationEventSubscription.Matching<InboundInteractionReceived>("assistant.#");

        published.Should().StartWith(agenda.RoutingKeyPattern!.TrimEnd('#'));
        published.Should().NotStartWith(assistant.RoutingKeyPattern!.TrimEnd('#'));
    }
}

public class IntegrationEventTypeRegistryTests
{
    [Fact]
    public void Resolves_a_name_back_to_its_type()
    {
        var registry = new IntegrationEventTypeRegistry([typeof(PasswordResetRequested)]);

        registry.TryResolve("identity.password-reset.v1", out var type).Should().BeTrue();
        type.Should().Be<PasswordResetRequested>();
    }

    [Fact]
    public void NameOf_returns_the_registered_name()
        => new IntegrationEventTypeRegistry([typeof(MfaEnabled)]).NameOf(typeof(MfaEnabled))
            .Should().Be("mfa-enabled");

    [Fact]
    public void NameOf_rejects_a_type_that_was_never_registered()
    {
        var act = () => new IntegrationEventTypeRegistry([]).NameOf(typeof(MfaEnabled));

        act.Should().Throw<InvalidOperationException>().WithMessage("*not a registered integration event*");
    }

    [Fact]
    public void An_unknown_name_does_not_resolve()
        => new IntegrationEventTypeRegistry([typeof(MfaEnabled)])
            .TryResolve("nope", out _).Should().BeFalse();

    [Fact]
    public void Two_types_claiming_one_name_is_a_startup_failure()
    {
        // Both fall back to convention and collide; silently dropping one is the failure mode
        // this guards against.
        var act = () => new IntegrationEventTypeRegistry([typeof(Duplicated.Clash), typeof(AlsoDuplicated.Clash)]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*claimed by both*");
    }

    [Fact]
    public void DiscoverIn_finds_the_concrete_events_of_an_assembly()
    {
        var found = IntegrationEventTypeRegistry.DiscoverIn(typeof(PasswordResetRequested).Assembly);

        found.Should().Contain(typeof(PasswordResetRequested)).And.Contain(typeof(InboundInteractionReceived));
    }
}

public class BrokerCapabilityValidationTests
{
    private static BrokerMessagingOptions Options(Action<BrokerMessagingOptions> configure)
    {
        var options = new BrokerMessagingOptions();
        configure(options);
        return options;
    }

    [Fact]
    public void A_wildcard_subscription_is_accepted_by_an_amqp_broker()
    {
        var options = Options(o => o.Subscribe<InboundInteractionReceived>("agenda.#"));

        var act = () => options.ValidateAgainst(BrokerCapabilities.Amqp, "RabbitMQ");

        act.Should().NotThrow();
    }

    [Fact]
    public void A_wildcard_subscription_is_rejected_by_a_broker_without_it()
    {
        var options = Options(o => o.Subscribe<InboundInteractionReceived>("agenda.#"));

        var act = () => options.ValidateAgainst(BrokerCapabilities.Broadcast, "SQS");

        act.Should().Throw<InvalidOperationException>().WithMessage("*wildcard routing*SQS does not support*");
    }

    [Fact]
    public void An_amqp_broker_advertises_every_routing_shape()
    {
        BrokerCapabilities.Amqp.HasFlag(BrokerCapabilities.WildcardRouting).Should().BeTrue();
        BrokerCapabilities.Amqp.HasFlag(BrokerCapabilities.HeaderRouting).Should().BeTrue();
    }

    [Fact]
    public void A_log_broker_advertises_broadcast_only()
    {
        // The topic is fixed per event type at registration, so a per-message routing key has
        // nowhere to live that the broker can filter on.
        BrokerCapabilities.Log.HasFlag(BrokerCapabilities.Broadcast).Should().BeTrue();
        BrokerCapabilities.Log.HasFlag(BrokerCapabilities.KeyedRouting).Should().BeFalse();
        BrokerCapabilities.Log.HasFlag(BrokerCapabilities.WildcardRouting).Should().BeFalse();
        BrokerCapabilities.Log.HasFlag(BrokerCapabilities.HeaderRouting).Should().BeFalse();
    }

    [Fact]
    public void A_routed_subscription_is_rejected_on_a_log_broker()
    {
        // The design decision this protects: silently degrading "only the owner wakes up" into
        // "everyone reads and filters" is exactly what routing exists to avoid.
        var options = Options(o => o.Subscribe<InboundInteractionReceived>("agenda.#"));

        var act = () => options.ValidateAgainst(BrokerCapabilities.Log, "Kafka");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Kafka does not support*");
    }
}
