using FluentAssertions;
using Pottmayer.Tars.Observability.Options;

namespace Pottmayer.Tars.Observability.Tests.Unit;

public class ObservabilityOptionsTests
{
    [Fact]
    public void Defaults_target_the_conventional_section_and_are_enabled()
    {
        var options = new ObservabilityOptions();

        ObservabilityOptions.SectionName.Should().Be("Tars:Observability");
        options.Enabled.Should().BeTrue();
        options.ServiceName.Should().BeEmpty();
        options.ServiceVersion.Should().BeNull();
        options.OtlpEndpoint.Should().BeNull();
    }
}

public class ObservabilityOptionsValidationTests
{
    [Fact]
    public void An_enabled_config_needs_a_service_name()
    {
        var options = new ObservabilityOptions { Enabled = true, ServiceName = "" };

        ObservabilityOptionsValidation.Validate(options).Should().BeFalse();
    }

    [Fact]
    public void An_enabled_config_with_a_service_name_is_valid()
    {
        var options = new ObservabilityOptions { Enabled = true, ServiceName = "pandora" };

        ObservabilityOptionsValidation.Validate(options).Should().BeTrue();
    }

    [Fact]
    public void A_disabled_config_is_valid_even_without_a_service_name()
    {
        var options = new ObservabilityOptions { Enabled = false, ServiceName = "" };

        ObservabilityOptionsValidation.Validate(options).Should().BeTrue();
    }
}
