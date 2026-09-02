# Communication — Registration Scenarios and Testing

Each scenario shows the complete registration. See [overview.md](./overview.md) for the contracts and
[configuration.md](./configuration.md) for the full options reference and [telegram.md](./telegram.md)
for the Bot API surface.

---

## Scenario 1 — E-mail, environment-selected provider

The common shape: logging (fake) sender in dev, MailKit in every other environment. Both bind to the
same `IEmailSender`, so the calling code never branches on environment.

```csharp
// Program.cs
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

```json
// appsettings.Production.json
{
  "Tars": {
    "Communication": {
      "Email": {
        "Smtp": {
          "Host": "smtp.sendgrid.net",
          "Port": 587,
          "UseStartTls": true,
          "Username": "apikey",
          "Password": "<from environment/secret, not committed>",
          "FromAddress": "no-reply@my-app.com",
          "FromName": "My App"
        }
      }
    }
  }
}
```

---

## Scenario 2 — Telegram bot (send + long polling)

```csharp
// Program.cs
builder.AddTarsTelegramOptions();
builder.Services.AddTarsTelegramClient();
```

```json
// appsettings.json
{
  "Tars": {
    "Communication": {
      "Telegram": {
        "BotToken": "<from environment, not committed>",
        "RequestTimeout": "00:00:30",
        "PollTimeoutGrace": "00:00:10"
      }
    }
  }
}
```

```csharp
public sealed class TelegramUpdatePump(ITelegramClient client, ILogger<TelegramUpdatePump> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long offset = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            var updates = await client.GetUpdatesAsync(offset, pollTimeout: TimeSpan.FromSeconds(25), stoppingToken);
            foreach (var update in updates)
            {
                offset = update.UpdateId + 1;
                // dispatch update.Message / update.CallbackQuery to a handler
            }
        }
    }
}
```

See [telegram.md](./telegram.md) for webhook mode as an alternative to long polling, inline keyboards,
and the permanent-vs-transient `TelegramException` distinction that governs retry.

---

## Scenario 3 — Both, sharing the same host

Email and Telegram are independent families; nothing links their registration order.

```csharp
// Program.cs
builder.AddTarsMailKitEmailOptions();
builder.Services.AddTarsMailKitEmailSender();

builder.AddTarsTelegramOptions();
builder.Services.AddTarsTelegramClient();
```

A module that needs to notify a user over "whichever channel they prefer" (e.g. Pandora's Channels
module) typically wraps both behind its own `INotificationDispatcher`-style port, choosing `IEmailSender`
or `ITelegramClient` per recipient preference — that port is application code, not part of this family.

---

## Testing — faking `IEmailSender` and `ITelegramClient`

**Email:** `AddTarsLoggingEmailSender()` *is* the intended test/dev fake — it satisfies `IEmailSender`,
never touches the network, and writes what it would have sent to the log. Prefer it over a hand-rolled
mock unless a test needs to assert on the exact `EmailMessage` sent, in which case implement
`IEmailSender` directly:

```csharp
public sealed class RecordingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        Sent.Add(message);
        return Task.FromResult(new EmailDeliveryResult("test", ProviderMessageId: null));
    }
}
```

**Telegram:** there is no built-in fake. `ITelegramClient` is a plain interface (`SendMessageAsync`,
`GetUpdatesAsync`, inline-keyboard and file-download methods, webhook management) — implement a
recording fake the same way as above for unit tests, or use `IHttpClientFactory`'s test handler
seam if you specifically need to exercise `TelegramBotClient`'s HTTP behavior (retry classification,
permanent-vs-transient `TelegramException`) rather than bypass it.
