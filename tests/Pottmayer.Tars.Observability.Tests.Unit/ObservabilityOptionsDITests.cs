using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Observability.DI;
using Pottmayer.Tars.Observability.Options;

namespace Pottmayer.Tars.Observability.Tests.Unit;

public class ObservabilityOptionsDITests
{
    private static HostApplicationBuilder BuilderWith(params (string Key, string Value)[] settings)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(
            settings.ToDictionary(s => s.Key, s => (string?)s.Value));

        return builder;
    }

    [Fact]
    public void Binds_settings_from_the_default_section()
    {
        var builder = BuilderWith(
            ("Tars:Observability:Enabled", "true"),
            ("Tars:Observability:ServiceName", "pandora"),
            ("Tars:Observability:ServiceVersion", "1.2.3"),
            ("Tars:Observability:OtlpEndpoint", "http://collector:4317"));

        builder.AddTarsObservabilityOptions();
        using var provider = builder.Services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;
        options.Enabled.Should().BeTrue();
        options.ServiceName.Should().Be("pandora");
        options.ServiceVersion.Should().Be("1.2.3");
        options.OtlpEndpoint.Should().Be("http://collector:4317");
    }

    [Fact]
    public void Defaults_the_service_name_to_the_application_name_when_left_empty()
    {
        var builder = BuilderWith(("Tars:Observability:OtlpEndpoint", "http://collector:4317"));

        builder.AddTarsObservabilityOptions();
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<ObservabilityOptions>>()
            .Value.ServiceName.Should().Be(builder.Environment.ApplicationName);
    }

    [Fact]
    public void Binds_from_a_custom_section_when_one_is_given()
    {
        var builder = BuilderWith(("Telemetry:ServiceName", "custom"));

        builder.AddTarsObservabilityOptions(sectionName: "Telemetry");
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value.ServiceName.Should().Be("custom");
    }

    [Fact]
    public void Applies_the_configure_callback_over_bound_values()
    {
        var builder = BuilderWith(("Tars:Observability:ServiceName", "from-config"));

        builder.AddTarsObservabilityOptions(configure: o => o.ServiceName = "from-callback");
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value.ServiceName.Should().Be("from-callback");
    }
}
