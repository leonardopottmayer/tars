using global::Serilog;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pottmayer.Tars.Observability.Serilog.DI;

namespace Pottmayer.Tars.Observability.Tests.Unit;

public class ObservabilitySerilogDITests
{
    [Fact]
    public void AddTarsSerilog_makes_serilog_the_logger_factory()
    {
        var services = new ServiceCollection();

        services.AddTarsSerilog(logger => logger.WriteTo.Console());

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ILoggerFactory>();

        factory.GetType().FullName.Should().Contain("Serilog");
    }

    [Fact]
    public void WriteToTarsOtlp_is_fluent_and_returns_the_same_configuration()
    {
        var configuration = new LoggerConfiguration();

        var result = configuration.WriteToTarsOtlp("pandora", "http://collector:4317");

        result.Should().BeSameAs(configuration);
    }

    [Fact]
    public void WriteToTarsOtlp_builds_a_working_logger_without_an_endpoint()
    {
        // No endpoint given: the sink must still build (it falls back to env/default), not throw.
        var act = () =>
        {
            using var logger = new LoggerConfiguration()
                .WriteToTarsOtlp("pandora")
                .CreateLogger();
        };

        act.Should().NotThrow();
    }
}
