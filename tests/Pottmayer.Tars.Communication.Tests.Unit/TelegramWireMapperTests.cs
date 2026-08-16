using System.Text.Json;
using FluentAssertions;
using Pottmayer.Tars.Communication.Telegram;
using Pottmayer.Tars.Communication.Telegram.Abstractions.Models;
using Pottmayer.Tars.Communication.Telegram.Wire;

namespace Pottmayer.Tars.Communication.Tests.Unit;

public class TelegramErrorClassifierTests
{
    [Theory]
    [InlineData(400)] // malformed request, "chat not found"
    [InlineData(401)] // bad bot token
    [InlineData(403)] // bot blocked, user deactivated
    [InlineData(404)]
    [InlineData(409)] // getUpdates racing a registered webhook
    public void Client_errors_are_permanent(int errorCode)
        => TelegramErrorClassifier.IsPermanent(errorCode).Should().BeTrue();

    [Theory]
    [InlineData(429)] // rate limited — the one 4xx worth repeating
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(null)] // transport failure: nothing says the request was bad
    public void Rate_limits_server_errors_and_transport_failures_are_transient(int? errorCode)
        => TelegramErrorClassifier.IsPermanent(errorCode).Should().BeFalse();
}

public class TelegramWireMapperTests
{
    private static WireMessage Message(Action<WireMessage>? configure = null)
    {
        var message = new WireMessage
        {
            MessageId = 42,
            Chat = new WireChat { Id = 987, Type = "private" },
            From = new WireUser { Id = 987, Username = "leo", FirstName = "Leonardo", LanguageCode = "pt-br" },
            Date = 1_755_000_000,
        };

        configure?.Invoke(message);
        return message;
    }

    [Fact]
    public void ToIncomingMessage_maps_the_chat_sender_and_timestamp()
    {
        var mapped = TelegramWireMapper.ToIncomingMessage(Message(m => m.Text = "oi"));

        mapped.MessageId.Should().Be(42);
        mapped.Chat.Should().Be(new TelegramChat(987, "private"));
        mapped.From!.Username.Should().Be("leo");
        mapped.From.LanguageCode.Should().Be("pt-br");
        mapped.Text.Should().Be("oi");
        mapped.SentAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1_755_000_000));
    }

    [Fact]
    public void ToIncomingMessage_surfaces_a_caption_as_the_text_of_a_media_message()
    {
        var wire = Message(m =>
        {
            m.Caption = "olha isso";
            m.Photo = [new WireFile { FileId = "small" }, new WireFile { FileId = "large" }];
        });

        TelegramWireMapper.ToIncomingMessage(wire).Text.Should().Be("olha isso");
    }

    [Fact]
    public void ToIncomingMessage_carries_the_replied_to_message_id()
    {
        var wire = Message(m => m.ReplyToMessage = new WireMessage { MessageId = 7 });

        TelegramWireMapper.ToIncomingMessage(wire).ReplyToMessageId.Should().Be(7);
    }

    [Fact]
    public void ToMedia_maps_a_voice_note_with_its_mime_type_and_duration()
    {
        var wire = Message(m => m.Voice = new WireFile
        {
            FileId = "voice-1", MimeType = "audio/ogg", Duration = 3, FileSize = 4096,
        });

        var media = TelegramWireMapper.ToMedia(wire);

        media.Should().Be(new TelegramMedia(TelegramMediaKind.Voice, "voice-1", "audio/ogg", 3, 4096));
    }

    [Fact]
    public void ToMedia_picks_the_largest_available_photo_size()
    {
        var wire = Message(m => m.Photo =
        [
            new WireFile { FileId = "thumb", FileSize = 100 },
            new WireFile { FileId = "medium", FileSize = 5_000 },
            new WireFile { FileId = "original", FileSize = 90_000 },
        ]);

        var media = TelegramWireMapper.ToMedia(wire)!;

        media.Kind.Should().Be(TelegramMediaKind.Photo);
        media.FileId.Should().Be("original");
    }

    [Fact]
    public void ToMedia_returns_null_for_a_plain_text_message()
        => TelegramWireMapper.ToMedia(Message(m => m.Text = "sem anexo")).Should().BeNull();

    [Fact]
    public void ToCallbackQuery_maps_the_data_and_the_message_it_belongs_to()
    {
        var wire = new WireCallbackQuery
        {
            Id = "cbq-1",
            From = new WireUser { Id = 987, Username = "leo" },
            Data = "0193c8f2",
            Message = Message(),
        };

        var mapped = TelegramWireMapper.ToCallbackQuery(wire);

        mapped.Id.Should().Be("cbq-1");
        mapped.Data.Should().Be("0193c8f2");
        mapped.From.Id.Should().Be(987);
        mapped.Chat.Should().Be(new TelegramChat(987, "private"));
        mapped.MessageId.Should().Be(42);
    }

    [Fact]
    public void ToUpdate_sets_only_the_branch_the_update_carries()
    {
        var messageUpdate = TelegramWireMapper.ToUpdate(new WireUpdate { UpdateId = 1, Message = Message() });
        var callbackUpdate = TelegramWireMapper.ToUpdate(new WireUpdate
        {
            UpdateId = 2,
            CallbackQuery = new WireCallbackQuery { Id = "c", From = new WireUser { Id = 1 } },
        });

        messageUpdate.Message.Should().NotBeNull();
        messageUpdate.CallbackQuery.Should().BeNull();
        callbackUpdate.CallbackQuery.Should().NotBeNull();
        callbackUpdate.Message.Should().BeNull();
    }

    [Fact]
    public void ToUpdate_leaves_both_branches_null_for_an_update_kind_that_is_not_modelled()
    {
        var update = TelegramWireMapper.ToUpdate(new WireUpdate { UpdateId = 3 });

        update.UpdateId.Should().Be(3);
        update.Message.Should().BeNull();
        update.CallbackQuery.Should().BeNull();
    }

    [Fact]
    public void ToSendRequest_maps_the_parse_mode_and_the_reply_target()
    {
        var message = new TelegramMessage(
            "987", "oi", TelegramParseMode.MarkdownV2, ReplyToMessageId: 42, DisableLinkPreview: true);

        var request = TelegramWireMapper.ToSendRequest(message);

        request.ChatId.Should().Be("987");
        request.ParseMode.Should().Be("MarkdownV2");
        request.ReplyParameters!.MessageId.Should().Be(42);
        request.LinkPreviewOptions!.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public void ToSendRequest_leaves_untouched_options_null_so_they_stay_off_the_wire()
    {
        var request = TelegramWireMapper.ToSendRequest(new TelegramMessage("987", "oi"));

        request.ParseMode.Should().BeNull();
        request.DisableNotification.Should().BeNull();
        request.LinkPreviewOptions.Should().BeNull();
        request.ReplyParameters.Should().BeNull();
        request.ReplyMarkup.Should().BeNull();
    }

    [Fact]
    public void ToSendRequest_maps_the_keyboard_rows_in_order()
    {
        var keyboard = InlineKeyboard.Stacked(
            InlineButton.Callback("Feito", "a"),
            InlineButton.Link("Abrir", new Uri("https://tars.local/1")));

        var request = TelegramWireMapper.ToSendRequest(new TelegramMessage("987", "oi", Keyboard: keyboard));

        request.ReplyMarkup!.InlineKeyboard.Should().HaveCount(2);
        request.ReplyMarkup.InlineKeyboard[0][0].CallbackData.Should().Be("a");
        request.ReplyMarkup.InlineKeyboard[0][0].Url.Should().BeNull();
        request.ReplyMarkup.InlineKeyboard[1][0].Url.Should().Be("https://tars.local/1");
        request.ReplyMarkup.InlineKeyboard[1][0].CallbackData.Should().BeNull();
    }

    [Fact]
    public void Wire_requests_serialize_to_snake_case_and_omit_nulls()
    {
        var request = TelegramWireMapper.ToSendRequest(
            new TelegramMessage("987", "oi", Keyboard: InlineKeyboard.SingleRow(InlineButton.Callback("Feito", "a"))));

        var json = JsonSerializer.Serialize(request, TelegramBotClient.Json);

        json.Should().Contain("\"chat_id\":\"987\"");
        json.Should().Contain("\"reply_markup\"");
        json.Should().Contain("\"inline_keyboard\"");
        json.Should().Contain("\"callback_data\":\"a\"");
        json.Should().NotContain("parse_mode");
        json.Should().NotContain("reply_parameters");
    }
}
