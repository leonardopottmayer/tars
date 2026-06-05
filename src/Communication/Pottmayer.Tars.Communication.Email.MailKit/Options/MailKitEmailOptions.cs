namespace Pottmayer.Tars.Communication.Email.MailKit.Options;

/// <summary>SMTP configuration for <see cref="MailKitEmailSender"/>.</summary>
public sealed class MailKitEmailOptions
{
    public const string SectionName = "Communication:Email:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;

    /// <summary>Use STARTTLS upgrade on the configured port; when false the transport auto-negotiates.</summary>
    public bool UseStartTls { get; set; } = true;

    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>Default sender address, used when an <see cref="Abstractions.EmailMessage"/> sets none.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Default sender display name.</summary>
    public string FromName { get; set; } = string.Empty;
}
