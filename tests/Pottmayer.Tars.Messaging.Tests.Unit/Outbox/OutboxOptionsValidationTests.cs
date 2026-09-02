using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.DI;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Options;

namespace Pottmayer.Tars.Messaging.Tests.Unit.Outbox;

public class OutboxOptionsValidationTests
{
    private static HostApplicationBuilder BuilderWith(params (string Key, string Value)[] settings)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(
            settings.ToDictionary(s => s.Key, s => (string?)s.Value));

        return builder;
    }

    [Fact]
    public void Defaults_target_conventional_section_and_are_valid()
    {
        var options = new OutboxOptions();

        OutboxOptions.SectionName.Should().Be("Tars:Messaging:Outbox");
        options.PollingInterval.Should().Be(TimeSpan.FromSeconds(5));
        options.BatchSize.Should().Be(100);
        options.MaxAttempts.Should().Be(8);
        options.LeaseDuration.Should().Be(TimeSpan.FromMinutes(5));
        options.PurgeEnabled.Should().BeTrue();
        options.RetentionPeriod.Should().Be(TimeSpan.FromDays(7));
        options.PurgeInterval.Should().Be(TimeSpan.FromHours(1));
        options.PurgeBatchSize.Should().Be(500);

        options.IsValid().Should().BeTrue();
        OutboxOptionsValidation.Validate(options).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Invalid_when_batch_size_is_not_positive(int batchSize)
    {
        var options = new OutboxOptions { BatchSize = batchSize };
        options.IsValid().Should().BeFalse();
        OutboxOptionsValidation.Validate(options).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Invalid_when_max_attempts_is_not_positive(int maxAttempts)
    {
        var options = new OutboxOptions { MaxAttempts = maxAttempts };
        options.IsValid().Should().BeFalse();
        OutboxOptionsValidation.Validate(options).Should().BeFalse();
    }

    [Fact]
    public void Invalid_when_polling_interval_is_not_positive()
    {
        var options = new OutboxOptions { PollingInterval = TimeSpan.Zero };
        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Invalid_when_lease_duration_is_not_positive()
    {
        var options = new OutboxOptions { LeaseDuration = TimeSpan.FromSeconds(-1) };
        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void OutboxDatabaseOptions_validates_database_key_and_backoff()
    {
        var dbOptions = new OutboxDatabaseOptions("identity");
        dbOptions.IsValid().Should().BeTrue();

        dbOptions.Backoff = null!;
        dbOptions.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Binds_outbox_options_from_configuration()
    {
        var builder = BuilderWith(
            ("Tars:Messaging:Outbox:BatchSize", "250"),
            ("Tars:Messaging:Outbox:MaxAttempts", "10"),
            ("Tars:Messaging:Outbox:PollingInterval", "00:00:02"));

        builder.AddTarsOutboxOptions();
        using var provider = builder.Services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<OutboxOptions>>().Value;
        options.BatchSize.Should().Be(250);
        options.MaxAttempts.Should().Be(10);
        options.PollingInterval.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Throws_on_startup_validation_when_options_are_invalid()
    {
        var builder = BuilderWith(("Tars:Messaging:Outbox:BatchSize", "0"));

        builder.AddTarsOutboxOptions();
        using var provider = builder.Services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<OutboxOptions>>().Value;
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*" + OutboxOptions.ValidationErrorMessage + "*");
    }
}
