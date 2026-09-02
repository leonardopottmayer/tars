namespace Pottmayer.Tars.Communication.Email.MailKit.Options;

/// <summary>SMTP configuration for <see cref="MailKitEmailSender"/>.</summary>
public sealed class MailKitEmailOptions
{
    /// <summary>Default configuration section these options bind from (<c>Tars:Communication:Email:Smtp</c>).</summary>
    public const string SectionName = "Tars:Communication:Email:Smtp";

    /// <summary>Message reported when validation fails on application start.</summary>
    public const string ValidationErrorMessage =
        "Invalid MailKitEmailOptions. Host is required; Port must be between 1 and 65535; FromAddress is required.";

    /// <summary>SMTP server host name.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP server port. Defaults to 587 (STARTTLS submission).</summary>
    public int Port { get; set; } = 587;

    /// <summary>Use STARTTLS upgrade on the configured port; when false the transport auto-negotiates.</summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>Optional SMTP username; when set, the transport authenticates before sending.</summary>
    public string? Username { get; set; }

    /// <summary>Optional SMTP password, used together with <see cref="Username"/>.</summary>
    public string? Password { get; set; }

    /// <summary>Default sender address, used when an <see cref="Abstractions.EmailMessage"/> sets none.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Default sender display name.</summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Returns <c>true</c> when the options are internally consistent: a host is present, the port is in
    /// the valid range (1–65535) and a default <see cref="FromAddress"/> is configured.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Host))
            return false;

        if (Port is < 1 or > 65535)
            return false;

        if (string.IsNullOrWhiteSpace(FromAddress))
            return false;

        return true;
    }
}
