# Communication Overview

## Projects in this family

- `Pottmayer.Tars.Communication.Email.Abstractions`
- `Pottmayer.Tars.Communication.Email`
- `Pottmayer.Tars.Communication.Email.MailKit`

## What the module offers

- a single `IEmailSender` contract for sending e-mail
- a logging fake sender for dev/tests (zero configuration)
- a real SMTP provider over MailKit, behind the same contract
- `EmailMessage` / `EmailDeliveryResult` value records

## The transport seam

Callers depend only on `IEmailSender`. Which implementation backs it is a **composition
choice** made at registration time — the logging fake or the MailKit SMTP provider — and the
calling code never changes. Both senders are registered with `TryAddSingleton`, so you
register exactly one (typically the fake in dev/tests and MailKit in production).

Implementations **throw on delivery failure** so the caller decides how to retry. The module
itself does not queue, retry or persist — pair it with your own durable queue/worker if you
need at-least-once delivery.

## Minimal registration

### Logging (fake) sender

```csharp
builder.Services.AddTarsLoggingEmailSender();
```

Writes the message to the log instead of delivering it. Default choice for dev and tests.

### MailKit (SMTP) sender

```csharp
builder.AddTarsMailKitEmailOptions();   // binds Communication:Email:Smtp
builder.Services.AddTarsMailKitEmailSender();
```

Delivers over SMTP. Pair `AddTarsMailKitEmailSender` with `AddTarsMailKitEmailOptions` so the
provider has its SMTP configuration. See [configuration.md](./configuration.md).

### Selecting a provider per environment

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTarsLoggingEmailSender();
}
else
{
    builder.AddTarsMailKitEmailOptions();
    builder.Services.AddTarsMailKitEmailSender();
}
```

## Sending an e-mail

```csharp
public sealed class WelcomeMailer(IEmailSender email)
{
    public Task SendAsync(string recipient, CancellationToken ct)
        => email.SendAsync(new EmailMessage(
            To: [recipient],
            Subject: "Welcome",
            Body: "<h1>Welcome aboard</h1>",
            IsHtml: true), ct);
}
```

`EmailMessage` fields:

- `To`: at least one recipient
- `Subject`, `Body`
- `IsHtml`: selects a `text/html` or `text/plain` body (default `false`)
- `Cc`: optional
- `FromAddress` / `FromName`: optional; when null the transport falls back to its configured
  default sender

`SendAsync` returns an `EmailDeliveryResult(Provider, ProviderMessageId)` — the provider that
accepted the message (`"logging"` or `"mailkit"`) and its message id, when available.

## Provider behavior (MailKit)

- Opens a **fresh SMTP connection per send**, so the sender is safe as a singleton.
- Uses STARTTLS when `UseStartTls` is set; otherwise auto-negotiates the secure options.
- **Authenticates only when a `Username` is configured** — leave it empty for an open relay
  such as a local Mailpit/MailHog.
- The sender (`From`) falls back to the configured `FromAddress` / `FromName` when the
  `EmailMessage` sets none.

## Main contracts

- `IEmailSender`
- `EmailMessage`
- `EmailDeliveryResult`

## Configuration

See [configuration.md](./configuration.md).
