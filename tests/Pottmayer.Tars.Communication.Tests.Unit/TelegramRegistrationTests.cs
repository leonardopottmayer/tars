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
    public void Factory_resolves_a_configured_bot_behind_the_contract()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddTarsTelegramOptions(configure: o => o.Bots["assistant"] = new TelegramBotOptions { BotToken = "123:ABC" });
        builder.Services.AddTarsTelegramHttpClient();
        builder.Services.AddTarsTelegramClientFactory();

        using var sp = builder.Services.BuildServiceProvider();

        var client = sp.GetRequiredService<ITelegramClientFactory>().GetClient("assistant");
        client.Should().BeOfType<TelegramBotClient>();
    }

    [Fact]
    public void Factory_resolves_bots_case_insensitively()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddTarsTelegramOptions(configure: o => o.Bots["assistant"] = new TelegramBotOptions { BotToken = "123:ABC" });
        builder.Services.AddTarsTelegramHttpClient();
        builder.Services.AddTarsTelegramClientFactory();

        using var sp = builder.Services.BuildServiceProvider();

        var act = () => sp.GetRequiredService<ITelegramClientFactory>().GetClient("Assistant");
        act.Should().NotThrow();
    }

    [Fact]
    public void Factory_hands_each_bot_its_own_configuration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tars:Communication:Telegram:Bots:notifications:BotToken"] = "111:NOTIF",
            ["Tars:Communication:Telegram:Bots:assistant:BotToken"] = "222:ASSIST",
            ["Tars:Communication:Telegram:Bots:assistant:RequestTimeout"] = "00:00:45",
        });

        builder.AddTarsTelegramOptions();
        builder.Services.AddTarsTelegramHttpClient();
        builder.Services.AddTarsTelegramClientFactory();

        using var sp = builder.Services.BuildServiceProvider();

        var factory = sp.GetRequiredService<ITelegramClientFactory>();
        factory.GetClient("notifications").Should().NotBeSameAs(factory.GetClient("assistant"));

        var bots = sp.GetRequiredService<IOptions<TelegramOptions>>().Value.Bots;
        bots["notifications"].BotToken.Should().Be("111:NOTIF");
        bots["assistant"].BotToken.Should().Be("222:ASSIST");
        bots["assistant"].RequestTimeout.Should().Be(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public void AddTarsTelegramHttpClient_shares_a_client_with_the_handler_timeout_disabled()
    {
        var services = new ServiceCollection();
        services.AddTarsTelegramHttpClient();

        using var sp = services.BuildServiceProvider();
        var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(TelegramServicesDI.HttpClientName);

        http.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void Factory_resolves_the_default_bot_when_called_without_a_name()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddTarsTelegramOptions(configure: o =>
            o.Bots[ITelegramClientFactory.DefaultBotName] = new TelegramBotOptions { BotToken = "123:ABC" });
        builder.Services.AddTarsTelegramHttpClient();
        builder.Services.AddTarsTelegramClientFactory();

        using var sp = builder.Services.BuildServiceProvider();

        var client = sp.GetRequiredService<ITelegramClientFactory>().GetClient();
        client.Should().BeOfType<TelegramBotClient>();
    }

    [Fact]
    public void Factory_without_a_name_throws_when_no_default_bot_is_configured()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddTarsTelegramOptions(configure: o =>
            o.Bots["assistant"] = new TelegramBotOptions { BotToken = "123:ABC" });
        builder.Services.AddTarsTelegramHttpClient();
        builder.Services.AddTarsTelegramClientFactory();

        using var sp = builder.Services.BuildServiceProvider();
        var factory = sp.GetRequiredService<ITelegramClientFactory>();

        var act = () => factory.GetClient();

        act.Should().Throw<TelegramException>().Which.IsPermanent.Should().BeTrue();
    }

    [Fact]
    public void Factory_throws_a_permanent_error_for_an_unconfigured_bot()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddTarsTelegramOptions();
        builder.Services.AddTarsTelegramHttpClient();
        builder.Services.AddTarsTelegramClientFactory();

        using var sp = builder.Services.BuildServiceProvider();
        var factory = sp.GetRequiredService<ITelegramClientFactory>();

        var act = () => factory.GetClient("nope");

        act.Should().Throw<TelegramException>().Which.IsPermanent.Should().BeTrue();
    }

    [Fact]
    public void AddTarsTelegramOptions_binds_the_bots_from_the_default_configuration_section()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tars:Communication:Telegram:Bots:notifications:BotToken"] = "123:ABC",
            ["Tars:Communication:Telegram:Bots:notifications:ApiBaseUrl"] = "https://api.telegram.local",
            ["Tars:Communication:Telegram:Bots:notifications:RequestTimeout"] = "00:00:45",
        });

        builder.AddTarsTelegramOptions();
        using var sp = builder.Services.BuildServiceProvider();

        var bot = sp.GetRequiredService<IOptions<TelegramOptions>>().Value.Bots["notifications"];
        bot.BotToken.Should().Be("123:ABC");
        bot.ApiBaseUrl.Should().Be("https://api.telegram.local");
        bot.RequestTimeout.Should().Be(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public void AddTarsTelegramOptions_binds_from_a_custom_section_when_one_is_given()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Bot:Bots:assistant:BotToken"] = "999:XYZ",
        });

        builder.AddTarsTelegramOptions(sectionName: "Bot");
        using var sp = builder.Services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<TelegramOptions>>().Value.Bots["assistant"].BotToken.Should().Be("999:XYZ");
    }

    [Fact]
    public void AddTarsTelegramOptions_applies_the_configure_callback_over_bound_values()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tars:Communication:Telegram:Bots:assistant:BotToken"] = "from-config",
        });

        builder.AddTarsTelegramOptions(configure: o => o.Bots["assistant"].BotToken = "from-callback");
        using var sp = builder.Services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<TelegramOptions>>().Value.Bots["assistant"].BotToken.Should().Be("from-callback");
    }
}

public class TelegramOptionsTests
{
    [Fact]
    public void Container_defaults_to_the_conventional_section_and_no_bots()
    {
        TelegramOptions.SectionName.Should().Be("Tars:Communication:Telegram");
        new TelegramOptions().Bots.Should().BeEmpty();
    }

    [Fact]
    public void Bot_defaults_target_the_public_bot_api()
    {
        var bot = new TelegramBotOptions();

        bot.ApiBaseUrl.Should().Be("https://api.telegram.org");
        bot.RequestTimeout.Should().Be(TimeSpan.FromSeconds(30));
        bot.PollTimeoutGrace.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void An_empty_set_of_bots_is_valid_but_a_tokenless_bot_is_not()
    {
        new TelegramOptions().IsValid().Should().BeTrue();

        var withEmptyBot = new TelegramOptions { Bots = { ["assistant"] = new TelegramBotOptions() } };
        withEmptyBot.IsValid().Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("/relative/only")]
    public void A_bot_with_a_non_http_base_url_is_invalid(string baseUrl)
        => new TelegramBotOptions { BotToken = "123:ABC", ApiBaseUrl = baseUrl }.IsValid().Should().BeFalse();
}
