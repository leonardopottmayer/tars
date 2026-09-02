using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.DI;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.Options;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.DI;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Options;
using RabbitMQ.Client;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

public class MassTransitRabbitMqOptionsBindingTests
{
    private static HostApplicationBuilder BuilderWith(params (string Key, string Value)[] settings)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(
            settings.ToDictionary(s => s.Key, s => (string?)s.Value));

        return builder;
    }

    [Fact]
    public void Binds_connection_settings_from_the_default_section()
    {
        var builder = BuilderWith(
            ("Tars:Messaging:RabbitMq:Host", "rabbit.prod"),
            ("Tars:Messaging:RabbitMq:Port", "5673"),
            ("Tars:Messaging:RabbitMq:VirtualHost", "/pandora"),
            ("Tars:Messaging:RabbitMq:Username", "app"),
            ("Tars:Messaging:RabbitMq:UseSsl", "true"),
            ("Tars:Messaging:RabbitMq:PrefetchCount", "1"));

        builder.AddTarsRabbitMqOptions();
        using var provider = builder.Services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<MassTransitRabbitMqMessagingOptions>>().Value;
        options.Host.Should().Be("rabbit.prod");
        options.Port.Should().Be(5673);
        options.VirtualHost.Should().Be("/pandora");
        options.Username.Should().Be("app");
        options.UseSsl.Should().BeTrue();
        options.PrefetchCount.Should().Be(1);
    }

    [Fact]
    public void Binds_the_nested_endpoint_name_which_is_the_queue()
    {
        var builder = BuilderWith(("Tars:Messaging:RabbitMq:Messaging:EndpointName", "channels"));

        builder.AddTarsRabbitMqOptions();
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<MassTransitRabbitMqMessagingOptions>>()
            .Value.Messaging.EndpointName.Should().Be("channels");
    }

    [Fact]
    public void Binds_the_retry_interval_as_a_timespan()
    {
        var builder = BuilderWith(("Tars:Messaging:RabbitMq:RetryInterval", "00:00:10"));

        builder.AddTarsRabbitMqOptions();
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<MassTransitRabbitMqMessagingOptions>>()
            .Value.RetryInterval.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Binds_from_a_custom_section_when_one_is_given()
    {
        var builder = BuilderWith(("Broker:Host", "custom.rabbit"));

        builder.AddTarsRabbitMqOptions(sectionName: "Broker");
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<MassTransitRabbitMqMessagingOptions>>().Value.Host.Should().Be("custom.rabbit");
    }

    [Fact]
    public void Applies_the_configure_callback_over_bound_values()
    {
        var builder = BuilderWith(("Tars:Messaging:RabbitMq:Host", "from-config"));

        builder.AddTarsRabbitMqOptions(configure: o => o.Host = "from-callback");
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<MassTransitRabbitMqMessagingOptions>>().Value.Host.Should().Be("from-callback");
    }

    [Fact]
    public async Task The_builder_overload_registers_the_provider_with_configuration_applied()
    {
        // The point of the binder: credentials come from appsettings, subscriptions stay in code.
        var builder = BuilderWith(
            ("Tars:Messaging:RabbitMq:Host", "rabbit.prod"),
            ("Tars:Messaging:RabbitMq:Messaging:EndpointName", "channels"));

        builder.AddTarsMassTransitRabbitMq(
            configure: o => o.Messaging.Subscribe<InboundInteractionReceived>("agenda.#"));

        await using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IIntegrationEventBus>().Should().NotBeNull();
    }

    [Fact]
    public void Defaults_target_the_conventional_section_and_a_topic_exchange()
    {
        var options = new MassTransitRabbitMqMessagingOptions();

        MassTransitRabbitMqMessagingOptions.SectionName.Should().Be("Tars:Messaging:RabbitMq");
        options.Port.Should().Be(5672);
        options.RoutedExchangeType.Should().Be(ExchangeType.Topic);
        options.IsValid().Should().BeTrue();
    }

    [Fact]
    public void Throws_on_startup_validation_when_options_are_invalid()
    {
        var builder = BuilderWith(("Tars:Messaging:RabbitMq:Host", ""));

        builder.AddTarsRabbitMqOptions();
        using var provider = builder.Services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<MassTransitRabbitMqMessagingOptions>>().Value;
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*" + MassTransitRabbitMqMessagingOptions.ValidationErrorMessage + "*");
    }
}

public class MassTransitKafkaOptionsBindingTests
{
    private static HostApplicationBuilder BuilderWith(params (string Key, string Value)[] settings)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(
            settings.ToDictionary(s => s.Key, s => (string?)s.Value));

        return builder;
    }

    [Fact]
    public void Binds_connection_settings_from_the_default_section()
    {
        var builder = BuilderWith(
            ("Tars:Messaging:Kafka:BootstrapServers", "kafka-1:9092,kafka-2:9092"),
            ("Tars:Messaging:Kafka:ConsumerGroup", "analytics"),
            ("Tars:Messaging:Kafka:AutoOffsetReset", "Latest"),
            ("Tars:Messaging:Kafka:ConcurrentMessageLimit", "1"));

        builder.AddTarsKafkaOptions();
        using var provider = builder.Services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<MassTransitKafkaMessagingOptions>>().Value;
        options.BootstrapServers.Should().Be("kafka-1:9092,kafka-2:9092");
        options.ConsumerGroup.Should().Be("analytics");
        options.AutoOffsetReset.Should().Be(Confluent.Kafka.AutoOffsetReset.Latest);
        options.ConcurrentMessageLimit.Should().Be(1);
    }

    [Fact]
    public void Applies_the_configure_callback_over_bound_values()
    {
        var builder = BuilderWith(("Tars:Messaging:Kafka:BootstrapServers", "from-config:9092"));

        builder.AddTarsKafkaOptions(configure: o => o.BootstrapServers = "from-callback:9092");
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<MassTransitKafkaMessagingOptions>>()
            .Value.BootstrapServers.Should().Be("from-callback:9092");
    }

    [Fact]
    public async Task The_builder_overload_registers_the_provider_with_configuration_applied()
    {
        var builder = BuilderWith(
            ("Tars:Messaging:Kafka:BootstrapServers", "kafka.prod:9092"),
            ("Tars:Messaging:Kafka:Messaging:EndpointName", "analytics"));

        builder.AddTarsMassTransitKafka(
            configure: o => o.Messaging.Subscribe<PasswordResetRequested>());

        await using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IIntegrationEventBus>().Should().NotBeNull();
    }

    [Fact]
    public void The_builder_overload_still_rejects_a_routed_subscription()
    {
        // Configuration must not become a way around the capability guard.
        var builder = BuilderWith(("Tars:Messaging:Kafka:BootstrapServers", "kafka:9092"));

        var act = () => builder.AddTarsMassTransitKafka(
            configure: o => o.Messaging.Subscribe<InboundInteractionReceived>("agenda.#"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Kafka does not support*");
    }

    [Fact]
    public void Defaults_target_the_conventional_section()
    {
        MassTransitKafkaMessagingOptions.SectionName.Should().Be("Tars:Messaging:Kafka");
        new MassTransitKafkaMessagingOptions().BootstrapServers.Should().Be("localhost:9092");
    }

    [Fact]
    public void Throws_on_startup_validation_when_options_are_invalid()
    {
        var builder = BuilderWith(("Tars:Messaging:Kafka:BootstrapServers", ""));

        builder.AddTarsKafkaOptions();
        using var provider = builder.Services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<MassTransitKafkaMessagingOptions>>().Value;
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*" + MassTransitKafkaMessagingOptions.ValidationErrorMessage + "*");
    }
}
