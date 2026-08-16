using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Communication.Telegram;
using Pottmayer.Tars.Communication.Telegram.Abstractions;
using Pottmayer.Tars.Communication.Telegram.DI;
using Pottmayer.Tars.Communication.Telegram.Options;

namespace Pottmayer.Tars.Communication.Tests.Unit;

public class TelegramRegistrationTests
{
    [Fact]
    public void AddTarsTelegramClient_resolves_the_bot_client_behind_the_contract()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddTarsTelegramOptions(configure: o => o.BotToken = "123:ABC");
        builder.Services.AddTarsTelegramClient();

        using var sp = builder.Services.BuildServiceProvider();

        sp.GetRequiredService<ITelegramClient>().Should().BeOfType<TelegramBotClient>();
    }

    [Fact]
    public void AddTarsTelegramClient_disables_the_handler_timeout_so_long_polling_survives()
    {
        var services = new ServiceCollection();
        services.AddTarsTelegramClient();

        using var sp = services.BuildServiceProvider();
        // AddHttpClient<TClient, TImplementation> names the logical client after the service type.
        var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(ITelegramClient));

        http.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void AddTarsTelegramOptions_binds_options_from_the_default_configuration_section()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tars:Communication:Telegram:BotToken"] = "123:ABC",
            ["Tars:Communication:Telegram:ApiBaseUrl"] = "https://api.telegram.local",
            ["Tars:Communication:Telegram:RequestTimeout"] = "00:00:45",
        });

        builder.AddTarsTelegramOptions();
        using var sp = builder.Services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
        options.BotToken.Should().Be("123:ABC");
        options.ApiBaseUrl.Should().Be("https://api.telegram.local");
        options.RequestTimeout.Should().Be(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public void AddTarsTelegramOptions_binds_from_a_custom_section_when_one_is_given()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Bot:BotToken"] = "999:XYZ",
        });

        builder.AddTarsTelegramOptions(sectionName: "Bot");
        using var sp = builder.Services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<TelegramOptions>>().Value.BotToken.Should().Be("999:XYZ");
    }

    [Fact]
    public void AddTarsTelegramOptions_applies_the_configure_callback_over_bound_values()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tars:Communication:Telegram:BotToken"] = "from-config",
        });

        builder.AddTarsTelegramOptions(configure: o => o.BotToken = "from-callback");
        using var sp = builder.Services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<TelegramOptions>>().Value.BotToken.Should().Be("from-callback");
    }
}

public class TelegramOptionsTests
{
    [Fact]
    public void Defaults_target_the_conventional_section_and_the_public_bot_api()
    {
        var options = new TelegramOptions();

        TelegramOptions.SectionName.Should().Be("Tars:Communication:Telegram");
        options.ApiBaseUrl.Should().Be("https://api.telegram.org");
        options.RequestTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.PollTimeoutGrace.Should().Be(TimeSpan.FromSeconds(10));
    }
}
