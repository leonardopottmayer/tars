using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Pottmayer.Tars.Communication.Email;
using Pottmayer.Tars.Communication.Email.Abstractions;

namespace Pottmayer.Tars.Communication.Tests.Unit;

public class LoggingEmailSenderTests
{
    private static EmailMessage Message()
        => new(["to@pandora.local"], "Subject", "Body", IsHtml: false);

    [Fact]
    public async Task SendAsync_reports_the_logging_provider_and_a_message_id()
    {
        var sender = new LoggingEmailSender(Mock.Of<ILogger<LoggingEmailSender>>());

        var result = await sender.SendAsync(Message());

        result.Provider.Should().Be(LoggingEmailSender.ProviderName);
        result.ProviderMessageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SendAsync_logs_all_recipients_and_cc_addresses()
    {
        var logger = new Mock<ILogger<LoggingEmailSender>>();
        var message = new EmailMessage(
            To: ["a@pandora.local", "b@pandora.local"],
            Subject: "Subject",
            Body: "Body",
            Cc: ["cc@pandora.local"]);

        var result = await new LoggingEmailSender(logger.Object).SendAsync(message);

        result.Provider.Should().Be(LoggingEmailSender.ProviderName);
        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("a@pandora.local")
                    && v.ToString()!.Contains("b@pandora.local")
                    && v.ToString()!.Contains("cc@pandora.local")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_writes_the_message_to_the_log()
    {
        var logger = new Mock<ILogger<LoggingEmailSender>>();

        await new LoggingEmailSender(logger.Object).SendAsync(Message());

        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
