using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Communication.Email;
using Pottmayer.Tars.Communication.Email.Abstractions;
using Pottmayer.Tars.Communication.Email.DI;
using Pottmayer.Tars.Communication.Email.MailKit;
using Pottmayer.Tars.Communication.Email.MailKit.DI;
using Pottmayer.Tars.Communication.Email.MailKit.Options;

namespace Pottmayer.Tars.Communication.Tests.Unit;

public class EmailSenderRegistrationTests
{
    [Fact]
    public void AddTarsLoggingEmailSender_registers_the_logging_sender_as_a_singleton()
    {
        var services = new ServiceCollection();

        services.AddTarsLoggingEmailSender();

        var descriptor = services.Single(d => d.ServiceType == typeof(IEmailSender));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be<LoggingEmailSender>();
    }

    [Fact]
    public void AddTarsMailKitEmailSender_registers_the_mailkit_sender_as_a_singleton()
    {
        var services = new ServiceCollection();

        services.AddTarsMailKitEmailSender();

        var descriptor = services.Single(d => d.ServiceType == typeof(IEmailSender));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be<MailKitEmailSender>();
    }

    [Fact]
    public void AddTarsMailKitEmailOptions_binds_options_from_the_default_configuration_section()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Communication:Email:Smtp:Host"] = "smtp.pandora.local",
            ["Communication:Email:Smtp:Port"] = "2525",
            ["Communication:Email:Smtp:Username"] = "mailer",
            ["Communication:Email:Smtp:FromAddress"] = "no-reply@pandora.local",
        });

        builder.AddTarsMailKitEmailOptions();
        using var sp = builder.Services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<MailKitEmailOptions>>().Value;
        options.Host.Should().Be("smtp.pandora.local");
        options.Port.Should().Be(2525);
        options.Username.Should().Be("mailer");
        options.FromAddress.Should().Be("no-reply@pandora.local");
    }

    [Fact]
    public void AddTarsMailKitEmailOptions_binds_from_a_custom_section_when_one_is_given()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "custom.pandora.local",
        });

        builder.AddTarsMailKitEmailOptions(sectionName: "Smtp");
        using var sp = builder.Services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<MailKitEmailOptions>>().Value.Host.Should().Be("custom.pandora.local");
    }

    [Fact]
    public void AddTarsMailKitEmailOptions_applies_the_configure_callback_over_bound_values()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Communication:Email:Smtp:Host"] = "from-config.pandora.local",
        });

        builder.AddTarsMailKitEmailOptions(configure: o => o.Host = "from-callback.pandora.local");
        using var sp = builder.Services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<MailKitEmailOptions>>().Value.Host.Should().Be("from-callback.pandora.local");
    }
}

public class MailKitEmailOptionsTests
{
    [Fact]
    public void Defaults_target_the_conventional_section_and_starttls_submission_port()
    {
        var options = new MailKitEmailOptions();

        MailKitEmailOptions.SectionName.Should().Be("Communication:Email:Smtp");
        options.Port.Should().Be(587);
        options.UseStartTls.Should().BeTrue();
    }
}
