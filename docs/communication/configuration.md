# Communication Configuration

## `MailKitEmailOptions`

Only the MailKit provider reads configuration; the logging sender needs none.

Section name:

```json
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

- `sectionName`: overrides the default `Communication:Email:Smtp`
- `configure`: runs after binding, so it overrides values read from configuration

## Local development with Mailpit

A common dev setup points MailKit at a local [Mailpit](https://mailpit.axllent.org/)
container (SMTP on `1025`, web UI on `8025`) with no authentication:

```json
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
```

## Notes

- The logging sender (`AddTarsLoggingEmailSender`) ignores this section entirely.
- Both senders register `IEmailSender` as a singleton via `TryAddSingleton`; register exactly
  one. Selecting which provider to register (e.g. by environment, or by a config key your host
  owns) is a composition concern of the consuming application, not of this module.
