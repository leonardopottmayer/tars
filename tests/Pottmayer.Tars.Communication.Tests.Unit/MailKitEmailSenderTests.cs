using FluentAssertions;
using MimeKit;
using Pottmayer.Tars.Communication.Email.Abstractions;
using Pottmayer.Tars.Communication.Email.MailKit;
using Pottmayer.Tars.Communication.Email.MailKit.Options;

namespace Pottmayer.Tars.Communication.Tests.Unit;

public class MailKitEmailSenderTests
{
    private static MailKitEmailOptions Settings() => new()
    {
        Host = "smtp.pandora.local",
        FromAddress = "no-reply@pandora.local",
        FromName = "Pandora",
    };

    [Fact]
    public void BuildMimeMessage_falls_back_to_the_configured_sender_when_the_message_sets_none()
    {
        var message = new EmailMessage(["to@pandora.local"], "Subject", "Body");

        var mime = MailKitEmailSender.BuildMimeMessage(message, Settings());

        var from = mime.From.Mailboxes.Single();
        from.Address.Should().Be("no-reply@pandora.local");
        from.Name.Should().Be("Pandora");
    }

    [Fact]
    public void BuildMimeMessage_prefers_the_message_sender_over_the_configured_default()
    {
        var message = new EmailMessage(
            ["to@pandora.local"], "Subject", "Body",
            FromAddress: "alerts@pandora.local", FromName: "Alerts");

        var mime = MailKitEmailSender.BuildMimeMessage(message, Settings());

        var from = mime.From.Mailboxes.Single();
        from.Address.Should().Be("alerts@pandora.local");
        from.Name.Should().Be("Alerts");
    }

    [Fact]
    public void BuildMimeMessage_maps_all_recipients_and_cc_addresses()
    {
        var message = new EmailMessage(
            To: ["a@pandora.local", "b@pandora.local"],
            Subject: "Subject",
            Body: "Body",
            Cc: ["cc@pandora.local"]);

        var mime = MailKitEmailSender.BuildMimeMessage(message, Settings());

        mime.To.Mailboxes.Select(m => m.Address)
            .Should().Equal("a@pandora.local", "b@pandora.local");
        mime.Cc.Mailboxes.Select(m => m.Address)
            .Should().Equal("cc@pandora.local");
    }

    [Fact]
    public void BuildMimeMessage_leaves_cc_empty_when_none_is_supplied()
    {
        var message = new EmailMessage(["to@pandora.local"], "Subject", "Body");

        var mime = MailKitEmailSender.BuildMimeMessage(message, Settings());

        mime.Cc.Count.Should().Be(0);
    }

    [Fact]
    public void BuildMimeMessage_sets_subject_and_body()
    {
        var message = new EmailMessage(["to@pandora.local"], "Welcome", "Hello there");

        var mime = MailKitEmailSender.BuildMimeMessage(message, Settings());

        mime.Subject.Should().Be("Welcome");
        ((TextPart)mime.Body).Text.Should().Be("Hello there");
    }

    [Theory]
    [InlineData(true, "text/html")]
    [InlineData(false, "text/plain")]
    public void BuildMimeMessage_picks_the_body_content_type_from_IsHtml(bool isHtml, string expectedMimeType)
    {
        var message = new EmailMessage(["to@pandora.local"], "Subject", "Body", IsHtml: isHtml);

        var mime = MailKitEmailSender.BuildMimeMessage(message, Settings());

        ((TextPart)mime.Body).ContentType.MimeType.Should().Be(expectedMimeType);
    }
}
