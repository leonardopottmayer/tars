using System.Text;
using FluentAssertions;
using Pottmayer.Tars.Communication.Telegram.Abstractions;
using Pottmayer.Tars.Communication.Telegram.Abstractions.Models;

namespace Pottmayer.Tars.Communication.Tests.Unit;

public class TelegramTextTests
{
    [Theory]
    [InlineData("_")]
    [InlineData("*")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("~")]
    [InlineData("`")]
    [InlineData(">")]
    [InlineData("#")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("=")]
    [InlineData("|")]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData(".")]
    [InlineData("!")]
    [InlineData("\\")]
    public void EscapeMarkdownV2_escapes_every_reserved_character(string reserved)
        => TelegramText.EscapeMarkdownV2(reserved).Should().Be(@"\" + reserved);

    [Fact]
    public void EscapeMarkdownV2_escapes_a_realistic_sentence()
    {
        var escaped = TelegramText.EscapeMarkdownV2("Regar as plantas (09:00) - urgente!");

        escaped.Should().Be(@"Regar as plantas \(09:00\) \- urgente\!");
    }

    [Fact]
    public void EscapeMarkdownV2_leaves_unreserved_characters_alone()
        => TelegramText.EscapeMarkdownV2("Bom dia, Leonardo 123 çãé").Should().Be("Bom dia, Leonardo 123 çãé");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EscapeMarkdownV2_returns_empty_for_no_text(string? text)
        => TelegramText.EscapeMarkdownV2(text).Should().BeEmpty();

    [Fact]
    public void EscapeHtml_replaces_the_three_characters_the_bot_api_subset_reserves()
        => TelegramText.EscapeHtml("<b>a & b</b>").Should().Be("&lt;b&gt;a &amp; b&lt;/b&gt;");

    [Fact]
    public void EscapeHtml_escapes_the_ampersand_before_the_angle_brackets()
        => TelegramText.EscapeHtml("&lt;").Should().Be("&amp;lt;");
}

public class TelegramCommandTests
{
    [Fact]
    public void TryParse_reads_a_bare_command()
    {
        TelegramCommand.TryParse("/status", out var command, out var argument).Should().BeTrue();

        command.Should().Be("status");
        argument.Should().BeNull();
    }

    [Fact]
    public void TryParse_reads_a_command_with_an_argument()
    {
        TelegramCommand.TryParse("/start abc123", out var command, out var argument).Should().BeTrue();

        command.Should().Be("start");
        argument.Should().Be("abc123");
    }

    [Fact]
    public void TryParse_strips_the_addressed_bot_suffix_used_in_groups()
    {
        TelegramCommand.TryParse("/start@tars_bot abc123", out var command, out var argument).Should().BeTrue();

        command.Should().Be("start");
        argument.Should().Be("abc123");
    }

    [Fact]
    public void TryParse_lowercases_the_command_but_not_the_argument()
    {
        TelegramCommand.TryParse("/START AbC", out var command, out var argument).Should().BeTrue();

        command.Should().Be("start");
        argument.Should().Be("AbC");
    }

    [Fact]
    public void TryParse_keeps_the_whole_remainder_as_a_single_argument()
    {
        TelegramCommand.TryParse("/echo  two words ", out _, out var argument).Should().BeTrue();

        argument.Should().Be("two words");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hello")]
    [InlineData("not /a command")]
    [InlineData("/")]
    [InlineData("/@bot")]
    public void TryParse_rejects_anything_that_is_not_a_command(string? text)
    {
        TelegramCommand.TryParse(text, out var command, out var argument).Should().BeFalse();

        command.Should().BeNull();
        argument.Should().BeNull();
    }
}

public class TelegramWebhookTests
{
    [Fact]
    public void IsValidSecretToken_accepts_the_configured_secret()
        => TelegramWebhook.IsValidSecretToken("s3cr3t", "s3cr3t").Should().BeTrue();

    [Theory]
    [InlineData("s3cr3t", "other")]
    [InlineData("s3cr3t", "s3cr3")]
    [InlineData("s3cr3t", "s3cr3tt")]
    public void IsValidSecretToken_rejects_a_mismatch(string header, string expected)
        => TelegramWebhook.IsValidSecretToken(header, expected).Should().BeFalse();

    [Theory]
    [InlineData(null, "s3cr3t")]
    [InlineData("", "s3cr3t")]
    [InlineData("s3cr3t", null)]
    [InlineData("s3cr3t", "")]
    [InlineData(null, null)]
    public void IsValidSecretToken_fails_closed_when_either_side_is_missing(string? header, string? expected)
        => TelegramWebhook.IsValidSecretToken(header, expected).Should().BeFalse();

    [Fact]
    public void SecretTokenHeaderName_is_the_header_telegram_sends()
        => TelegramWebhook.SecretTokenHeaderName.Should().Be("X-Telegram-Bot-Api-Secret-Token");
}

public class InlineButtonTests
{
    [Fact]
    public void Callback_keeps_the_label_and_the_data()
    {
        var button = InlineButton.Callback("✓ Feito", "0193c8f2");

        button.Label.Should().Be("✓ Feito");
        button.CallbackData.Should().Be("0193c8f2");
        button.Url.Should().BeNull();
    }

    [Fact]
    public void Callback_accepts_data_of_exactly_the_maximum_size()
    {
        var data = new string('a', InlineButton.MaxCallbackDataBytes);

        InlineButton.Callback("Ok", data).CallbackData.Should().Be(data);
    }

    [Fact]
    public void Callback_rejects_data_one_byte_over_the_limit()
    {
        var data = new string('a', InlineButton.MaxCallbackDataBytes + 1);

        var act = () => InlineButton.Callback("Ok", data);

        act.Should().Throw<ArgumentException>().WithMessage("*65 UTF-8 bytes*at most 64*");
    }

    [Fact]
    public void Callback_measures_the_limit_in_bytes_not_characters()
    {
        // 33 characters, but 66 bytes once encoded — the limit Telegram actually enforces.
        var data = new string('é', 33);
        Encoding.UTF8.GetByteCount(data).Should().Be(66);

        var act = () => InlineButton.Callback("Ok", data);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("", "data")]
    [InlineData("  ", "data")]
    [InlineData("Ok", "")]
    public void Callback_rejects_an_empty_label_or_empty_data(string label, string data)
    {
        var act = () => InlineButton.Callback(label, data);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Link_keeps_the_url_and_carries_no_callback_data()
    {
        var button = InlineButton.Link("Abrir", new Uri("https://tars.local/tasks/1"));

        button.Url.Should().Be(new Uri("https://tars.local/tasks/1"));
        button.CallbackData.Should().BeNull();
    }
}

public class InlineKeyboardTests
{
    private static readonly InlineButton Done = InlineButton.Callback("Feito", "a");
    private static readonly InlineButton Snooze = InlineButton.Callback("Adiar", "b");

    [Fact]
    public void Stacked_puts_every_button_on_its_own_row()
    {
        var keyboard = InlineKeyboard.Stacked(Done, Snooze);

        keyboard.Rows.Should().HaveCount(2);
        keyboard.Rows.Should().OnlyContain(row => row.Count == 1);
    }

    [Fact]
    public void SingleRow_puts_every_button_side_by_side()
    {
        var keyboard = InlineKeyboard.SingleRow(Done, Snooze);

        keyboard.Rows.Should().ContainSingle();
        keyboard.Rows[0].Should().HaveCount(2);
    }
}
