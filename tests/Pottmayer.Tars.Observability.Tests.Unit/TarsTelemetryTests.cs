using FluentAssertions;
using Pottmayer.Tars.Observability.Abstractions;

namespace Pottmayer.Tars.Observability.Tests.Unit;

public class TarsTelemetryTests
{
    [Fact]
    public void Root_and_wildcard_follow_the_tars_naming_convention()
    {
        TarsTelemetry.RootName.Should().Be("Pottmayer.Tars");
        TarsTelemetry.Wildcard.Should().Be("Pottmayer.Tars.*");
    }

    [Fact]
    public void Name_prefixes_the_family_under_the_root()
    {
        TarsTelemetry.Name("Messaging").Should().Be("Pottmayer.Tars.Messaging");
    }

    [Fact]
    public void The_wildcard_matches_a_name_built_for_a_family()
    {
        // The whole point of the convention: one AddSource/AddMeter wildcard captures every family.
        var name = TarsTelemetry.Name("Finances");

        name.Should().StartWith(TarsTelemetry.Wildcard.TrimEnd('*'));
    }
}

public class TarsCorrelationTests
{
    [Fact]
    public void Header_and_property_names_are_the_conventional_values()
    {
        TarsCorrelation.HeaderName.Should().Be("X-Correlation-ID");
        TarsCorrelation.PropertyName.Should().Be("CorrelationId");
    }
}
