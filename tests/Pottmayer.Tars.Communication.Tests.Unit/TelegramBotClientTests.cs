using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Communication.Telegram;
using Pottmayer.Tars.Communication.Telegram.Abstractions;
using Pottmayer.Tars.Communication.Telegram.Abstractions.Models;
using Pottmayer.Tars.Communication.Telegram.Options;

namespace Pottmayer.Tars.Communication.Tests.Unit;

/// <summary>Serves canned Bot API responses and records what the client asked for.</summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public List<Uri> Requests { get; } = [];
    public List<string> Bodies { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);
        Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}

public class TelegramBotClientTests
{
    private static (TelegramBotClient Client, StubHttpMessageHandler Handler) Build(
        string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new StubHttpMessageHandler(status, body);
        var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        var options = Microsoft.Extensions.Options.Options.Create(new TelegramOptions
        {
            BotToken = "123:ABC",
            ApiBaseUrl = "https://api.telegram.local",
        });

        return (new TelegramBotClient(http, options), handler);
    }

    [Fact]
    public async Task SendMessageAsync_posts_to_the_bot_method_and_returns_the_message_id()
    {
        var (client, handler) = Build(
            """{"ok":true,"result":{"message_id":555,"date":1755000000,"chat":{"id":987,"type":"private"}}}""");

        var result = await client.SendMessageAsync(new TelegramMessage("987", "oi"));

        handler.Requests.Single().ToString().Should().Be("https://api.telegram.local/bot123:ABC/sendMessage");
        handler.Bodies.Single().Should().Contain("\"chat_id\":\"987\"").And.Contain("\"text\":\"oi\"");
        result.MessageId.Should().Be(555);
        result.ChatId.Should().Be("987");
        result.SentAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1_755_000_000));
    }

    [Fact]
    public async Task SendMessageAsync_marks_a_blocked_bot_as_permanent()
    {
        var (client, _) = Build(
            """{"ok":false,"error_code":403,"description":"Forbidden: bot was blocked by the user"}""",
            HttpStatusCode.Forbidden);

        var act = () => client.SendMessageAsync(new TelegramMessage("987", "oi"));

        var exception = (await act.Should().ThrowAsync<TelegramException>()).Which;
        exception.IsPermanent.Should().BeTrue();
        exception.ErrorCode.Should().Be(403);
        exception.Method.Should().Be("sendMessage");
        exception.Message.Should().Contain("bot was blocked by the user");
    }

    [Fact]
    public async Task SendMessageAsync_marks_a_rate_limit_as_transient_and_surfaces_retry_after()
    {
        var (client, _) = Build(
            """{"ok":false,"error_code":429,"description":"Too Many Requests","parameters":{"retry_after":30}}""",
            HttpStatusCode.TooManyRequests);

        var act = () => client.SendMessageAsync(new TelegramMessage("987", "oi"));

        var exception = (await act.Should().ThrowAsync<TelegramException>()).Which;
        exception.IsPermanent.Should().BeFalse();
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task SendMessageAsync_marks_a_server_error_as_transient()
    {
        var (client, _) = Build(
            """{"ok":false,"error_code":502,"description":"Bad Gateway"}""",
            HttpStatusCode.BadGateway);

        var act = () => client.SendMessageAsync(new TelegramMessage("987", "oi"));

        (await act.Should().ThrowAsync<TelegramException>()).Which.IsPermanent.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_treats_a_non_bot_api_body_as_transient()
    {
        var (client, _) = Build("<html>gateway timeout</html>", HttpStatusCode.GatewayTimeout);

        var act = () => client.SendMessageAsync(new TelegramMessage("987", "oi"));

        (await act.Should().ThrowAsync<TelegramException>()).Which.IsPermanent.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_fails_when_no_bot_token_is_configured()
    {
        var http = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, "{}"));
        var client = new TelegramBotClient(http, Microsoft.Extensions.Options.Options.Create(new TelegramOptions()));

        var act = () => client.SendMessageAsync(new TelegramMessage("987", "oi"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tars:Communication:Telegram:BotToken*");
    }

    [Fact]
    public async Task GetUpdatesAsync_sends_the_offset_and_the_poll_timeout_in_seconds()
    {
        var (client, handler) = Build("""{"ok":true,"result":[]}""");

        await client.GetUpdatesAsync(offset: 101, pollTimeout: TimeSpan.FromSeconds(30));

        handler.Requests.Single().ToString().Should().EndWith("/getUpdates");
        handler.Bodies.Single().Should().Contain("\"offset\":101").And.Contain("\"timeout\":30");
    }

    [Fact]
    public async Task GetUpdatesAsync_maps_a_text_message_and_a_callback_query()
    {
        var (client, _) = Build(
            """
            {"ok":true,"result":[
              {"update_id":1,"message":{"message_id":10,"date":1755000000,
                "chat":{"id":987,"type":"private"},
                "from":{"id":987,"username":"leo","is_bot":false},
                "text":"/start abc"}},
              {"update_id":2,"callback_query":{"id":"cbq","data":"0193c8f2",
                "from":{"id":987,"username":"leo"},
                "message":{"message_id":11,"date":1755000000,"chat":{"id":987,"type":"private"}}}}
            ]}
            """);

        var updates = await client.GetUpdatesAsync(0, TimeSpan.FromSeconds(1));

        updates.Should().HaveCount(2);
        updates[0].Message!.Text.Should().Be("/start abc");
        updates[0].Message!.From!.Username.Should().Be("leo");
        updates[1].CallbackQuery!.Data.Should().Be("0193c8f2");
        updates[1].CallbackQuery!.MessageId.Should().Be(11);
    }

    [Fact]
    public async Task GetUpdatesAsync_maps_a_voice_note_into_media()
    {
        var (client, _) = Build(
            """
            {"ok":true,"result":[
              {"update_id":1,"message":{"message_id":10,"date":1755000000,
                "chat":{"id":987,"type":"private"},
                "voice":{"file_id":"voice-1","mime_type":"audio/ogg","duration":4,"file_size":8192}}}
            ]}
            """);

        var media = (await client.GetUpdatesAsync(0, TimeSpan.FromSeconds(1)))[0].Message!.Media!;

        media.Kind.Should().Be(TelegramMediaKind.Voice);
        media.FileId.Should().Be("voice-1");
        media.MimeType.Should().Be("audio/ogg");
        media.DurationSeconds.Should().Be(4);
    }

    [Fact]
    public async Task GetUpdatesAsync_rejects_a_negative_poll_timeout()
    {
        var (client, _) = Build("""{"ok":true,"result":[]}""");

        var act = () => client.GetUpdatesAsync(0, TimeSpan.FromSeconds(-1));

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task AnswerCallbackQueryAsync_posts_the_callback_id()
    {
        var (client, handler) = Build("""{"ok":true,"result":true}""");

        await client.AnswerCallbackQueryAsync("cbq-1", "Feito!");

        handler.Requests.Single().ToString().Should().EndWith("/answerCallbackQuery");
        handler.Bodies.Single().Should().Contain("\"callback_query_id\":\"cbq-1\"").And.Contain("Feito!");
    }

    [Fact]
    public async Task SetWebhookAsync_posts_the_url_and_the_secret_token()
    {
        var (client, handler) = Build("""{"ok":true,"result":true}""");

        await client.SetWebhookAsync(new Uri("https://tars.local/hook"), "s3cr3t");

        handler.Bodies.Single().Should()
            .Contain("\"url\":\"https://tars.local/hook\"")
            .And.Contain("\"secret_token\":\"s3cr3t\"");
    }

    [Fact]
    public async Task DownloadFileAsync_reports_a_missing_file_path_as_permanent()
    {
        var (client, _) = Build("""{"ok":true,"result":{"file_id":"voice-1"}}""");

        var act = () => client.DownloadFileAsync("voice-1");

        var exception = (await act.Should().ThrowAsync<TelegramException>()).Which;
        exception.IsPermanent.Should().BeTrue();
        exception.Method.Should().Be("getFile");
    }
}
