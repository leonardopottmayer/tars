# Communication Configuration

## `MailKitEmailOptions`

Only the MailKit provider reads configuration; the logging sender needs none.

Section name:

```json
"Tars": {
  "Communication": {
    "Email": {
      "Smtp": {
        "Host": "localhost",
        "Port": 587,
        "UseStartTls": true,
        "Username": "mailer",
        "Password": "secret",
        "FromAddress": "no-reply@my-app.local",
        "FromName": "My App"
      }
    }
  }
}
```

Fields:

- `Host`: SMTP host. Default: empty (must be set)
- `Port`: SMTP port. Default: `587` (the STARTTLS submission port)
- `UseStartTls`: upgrade the connection with STARTTLS on the configured port; when `false` the
  transport auto-negotiates the secure options. Default: `true`
- `Username` / `Password`: SMTP credentials. When `Username` is empty the sender does **not**
  authenticate — leave it empty for a local Mailpit/MailHog relay
- `FromAddress`: default sender address, used when an `EmailMessage` sets none
- `FromName`: default sender display name

## Binding

`AddTarsMailKitEmailOptions` binds the options from configuration:

```csharp
builder.AddTarsMailKitEmailOptions();
```

It accepts a custom section name and a post-bind callback:

```csharp
builder.AddTarsMailKitEmailOptions(
    sectionName: "MyApp:Smtp",
    configure: o => o.FromName = "My App");
```

- `sectionName`: overrides the default `Tars:Communication:Email:Smtp`
- `configure`: runs after binding, so it overrides values read from configuration

## Local development with Mailpit

A common dev setup points MailKit at a local [Mailpit](https://mailpit.axllent.org/)
container (SMTP on `1025`, web UI on `8025`) with no authentication:

```json
"Tars": {
  "Communication": {
    "Email": {
      "Smtp": {
        "Host": "localhost",
        "Port": 1025,
        "UseStartTls": false,
        "Username": "",
        "FromAddress": "no-reply@my-app.local",
        "FromName": "My App"
      }
    }
  }
}
```

## `TelegramOptions`

Section name:

```json
"Tars": {
  "Communication": {
    "Telegram": {
      "BotToken": "123456:ABC-DEF...",
      "ApiBaseUrl": "https://api.telegram.org",
      "RequestTimeout": "00:00:30",
      "PollTimeoutGrace": "00:00:10"
    }
  }
}
```

Fields:

- `BotToken`: the token from BotFather. Required — calls fail with an `InvalidOperationException`
  when it is empty, rather than reaching Telegram with an unusable URL
- `ApiBaseUrl`: Bot API root. Default: `https://api.telegram.org`. Override only for a self-hosted
  Bot API server, which raises the file size limits — but note that `DownloadFileAsync` buffers the
  whole file in memory, which is only sound under the public API's 20 MB ceiling. Self-hosting to
  move large files needs a streaming download first; see [telegram.md](./telegram.md#media)
- `RequestTimeout`: per-request deadline for everything except long polling. Default: `30` seconds
- `PollTimeoutGrace`: slack added on top of the caller's poll timeout before the HTTP request itself
  is cancelled, so a long poll is never killed by its own transport. Default: `10` seconds

### Binding

```csharp
builder.AddTarsTelegramOptions();
```

Same shape as the MailKit binder — it accepts a custom `sectionName` and a post-bind `configure`
callback.

### Keeping the token out of configuration files

The bot token is a credential with no scoping: it can read and write every chat the bot is in. Supply
it through an environment variable (`Tars__Communication__Telegram__BotToken`) or a mounted secret,
not through a committed `appsettings.json`.

## Notes

- The logging sender (`AddTarsLoggingEmailSender`) ignores the SMTP section entirely.
- Both e-mail senders register `IEmailSender` as a singleton via `TryAddSingleton`; register exactly
  one. Selecting which provider to register (e.g. by environment, or by a config key your host
  owns) is a composition concern of the consuming application, not of this module.
- `AddTarsTelegramClient` registers a typed `HttpClient` with its handler timeout disabled; the
  per-call deadlines above replace it. Do not set a handler timeout on top, or long polling breaks.
