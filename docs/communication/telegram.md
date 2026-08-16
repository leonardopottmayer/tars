# Telegram

## Projects

- `Pottmayer.Tars.Communication.Telegram.Abstractions`
- `Pottmayer.Tars.Communication.Telegram`

Two projects, not three: the e-mail family splits `Email` / `Email.MailKit` because SMTP has many
possible providers, and Telegram has exactly one — the Bot API.

## What the module offers

- an `ITelegramClient` contract covering sending, inline keyboards, long polling, file download and
  webhook management
- normalized inbound models (`TelegramUpdate`, `TelegramIncomingMessage`, `TelegramCallbackQuery`,
  `TelegramMedia`) with no JSON concerns leaking into them
- a `TelegramException` that says whether a failure is **permanent**
- `TelegramText` escaping for MarkdownV2 and the HTML subset
- `TelegramWebhook.IsValidSecretToken` for the webhook endpoint
- `TelegramCommand.TryParse` for the `/command@bot argument` grammar

## What it deliberately does not offer

It is transport plus models. It has **no** queue, **no** retry policy, **no** rate limiter, **no**
polling offset and **no** templates. Those are application concerns, and the polling offset in
particular is *state* — this block is stateless so it can be a singleton.

## What is not covered yet

The Bot API is large and this covers the part that has a consumer. Nothing below is blocked by the
design — each is an additive change to the contract and the client — but none of it exists today:

| Area | Missing |
|---|---|
| Sending | `editMessageText`, `deleteMessage`, `sendChatAction` (the "typing…" indicator) |
| Outbound media | Everything: `sendPhoto`, `sendDocument`, `sendAudio`, `sendVoice`. Media is received, never sent |
| Keyboards | `ReplyKeyboardMarkup`. Only inline keyboards are modelled |
| Callbacks | `show_alert` and `cache_time` on `answerCallbackQuery` |
| Update kinds | `edited_message`, `channel_post`, `inline_query`, `poll`, and `my_chat_member` — which is how a bot learns it was added to or removed from a group |
| Introspection | `getMe` |

Two of these bite specific shapes of bot: **editing a message in place** is the usual response to an
inline-button press, and **`my_chat_member`** is how a group bot tracks its own membership. A
one-to-one notification bot needs neither, which is why they are not here.

Large file downloads are a separate limit — see [Media](#media).

## Registration

```csharp
builder.AddTarsTelegramOptions();          // binds Tars:Communication:Telegram
builder.Services.AddTarsTelegramClient();
```

`AddTarsTelegramClient` registers `TelegramBotClient` as `ITelegramClient` on a typed `HttpClient`
whose handler timeout is **disabled on purpose** — every call sets its own deadline, because a long
poll legitimately waits longer than any sane default and would otherwise be cancelled by its own
transport.

## Sending

```csharp
public sealed class TaskReminder(ITelegramClient telegram)
{
    public Task NotifyAsync(string chatId, string title, string interactionId, CancellationToken ct)
        => telegram.SendMessageAsync(new TelegramMessage(
            ChatId: chatId,
            Text: $"*{TelegramText.EscapeMarkdownV2(title)}* vence agora",
            ParseMode: TelegramParseMode.MarkdownV2,
            Keyboard: InlineKeyboard.SingleRow(
                InlineButton.Callback("✓ Feito", interactionId),
                InlineButton.Callback("Adiar 1h", $"{interactionId}s"))), ct);
}
```

`SendMessageAsync` returns a `TelegramSendResult(ChatId, MessageId, SentAt)`. Keep the `MessageId` if
you want to correlate a later threaded reply back to what it answered — an incoming message carries
`ReplyToMessageId`.

### Escaping is not optional

Any text that came from a user or a template and is sent with a parse mode must go through
`TelegramText`. An unescaped `.` or `-` is enough for Telegram to reject the whole message with
`400 Bad Request`:

```csharp
TelegramText.EscapeMarkdownV2("Regar as plantas (09:00) - urgente!");
// Regar as plantas \(09:00\) \- urgente\!
```

### Callback data is 64 bytes

`InlineButton.Callback` throws when the data exceeds `InlineButton.MaxCallbackDataBytes` (64, counted
in **UTF-8 bytes**, not characters). This is a hard Bot API limit, and the guard exists to turn a
runtime rejection into a caller bug at the point of construction.

The limit is small on purpose: `callback_data` is meant to hold a **key**, not a payload. Store what
the button means in your own table and put the row's id here.

## Failure handling

Every failure — transport, HTTP status or `ok: false` — arrives as a `TelegramException`:

| Member | Meaning |
|---|---|
| `IsPermanent` | Retrying can never succeed. Stop and record the failure. |
| `ErrorCode` | The Bot API `error_code`, when the failure came from Telegram rather than the wire. |
| `RetryAfter` | From `parameters.retry_after` on a 429. Null when Telegram did not say. |
| `Method` | Which Bot API method failed. |

The classification rule: **every 4xx except 429 is permanent.** The request itself is the problem, so
repeating it verbatim reproduces the failure — `400 chat not found`, `401` for a bad token,
`403 bot was blocked by the user`, `409 Conflict` from a webhook competing with long polling. `429`
is rate limiting and `5xx` is Telegram having a bad minute; both are transient, as is any transport
failure.

A permanent failure on `sendMessage` usually means the address is dead, not that the message was bad
— that is the signal to disable the channel for that user, not just to drop one message.

Rate limiting is surfaced, not handled: Telegram allows roughly 30 messages/second overall and one
per second per chat. Honour `RetryAfter` in whatever backoff your queue already has instead of
building a second limiter here.

## Receiving

### Long polling

```csharp
var updates = await telegram.GetUpdatesAsync(offset: lastSeenUpdateId + 1, pollTimeout: TimeSpan.FromSeconds(30), ct);
```

`getUpdates` with a timeout **hangs** until an update arrives or the timeout elapses — it is not
short polling in a loop, and its latency is close to a webhook's.

Three constraints the caller owns:

- **One consumer per bot token.** `getUpdates` and `setWebhook` are mutually exclusive, and a second
  concurrent caller gets `409 Conflict` (a permanent `TelegramException`). Run the poller as a
  singleton; a second replica needs leader election.
- **The offset is the acknowledgement.** Passing `lastSeenUpdateId + 1` confirms every earlier
  update, and Telegram will never send them again. Persist the offset in the same transaction that
  stores the update, and process afterwards — then a crash reprocesses instead of losing.
- **Telegram retains unconfirmed updates for 24 hours**, so an outage of a few hours loses nothing.

### Webhook

```csharp
await telegram.SetWebhookAsync(new Uri("https://app.example/api/telegram/webhook"), secretToken, ct);
```

Telegram echoes `secretToken` on every request in the `X-Telegram-Bot-Api-Secret-Token` header. The
endpoint is anonymous, so validating it is the only thing authenticating the caller:

```csharp
var header = Request.Headers[TelegramWebhook.SecretTokenHeaderName].ToString();
if (!TelegramWebhook.IsValidSecretToken(header, options.SecretToken))
    return Unauthorized();
```

`IsValidSecretToken` compares in constant time and returns false when either side is missing, so a
misconfigured endpoint fails closed.

`DeleteWebhookAsync` removes the registration, which is what re-enables long polling.

### Reading an update

```csharp
foreach (var update in updates)
{
    if (update.CallbackQuery is { } callback)
    {
        // callback.Data is the button's callback_data — resolve it against your own table.
        await telegram.AnswerCallbackQueryAsync(callback.Id, ct: ct);
    }
    else if (update.Message is { } message)
    {
        if (TelegramCommand.TryParse(message.Text, out var command, out var argument))
        {
            // "/start abc123" -> command "start", argument "abc123"
        }
        else if (message.Media is { Kind: TelegramMediaKind.Voice } voice)
        {
            await using var audio = await telegram.DownloadFileAsync(voice.FileId, ct);
        }
    }
}
```

`AnswerCallbackQueryAsync` must be called within a few seconds of receiving a callback, whatever the
outcome of handling it — otherwise the client keeps showing a spinner.

Both branches can be null on an update kind this transport does not model (an edited message, a chat
member change). Confirm and ignore those rather than failing on them.

### Media

An incoming message carries at most one `TelegramMedia`, whatever field Telegram used: a voice note,
audio, video, video note, document or photo. For photos, which arrive as every available size, the
largest is selected. `FileId` is opaque and bot-specific — hand it back to `DownloadFileAsync`.

`DownloadFileAsync` **buffers the whole file into memory**, and the caller owns the returned stream.

That is sound against the public Bot API, which caps downloads at 20 MB — buffering keeps the HTTP
response fully disposed before the caller touches the stream, rather than leaving a connection open
on an ownership technicality.

The ceiling is the entire justification, so it stops holding the moment you point `ApiBaseUrl` at a
**self-hosted Bot API server**, where the download limit rises to 2 GB. Buffering a 500 MB video is
an `OutOfMemoryException`, not a slow download. Today this method is for small attachments — voice
notes, photos, documents — and moving large files needs a streaming overload that does not exist yet.

## Main contracts

- `ITelegramClient`
- `TelegramMessage`, `InlineKeyboard`, `InlineButton`, `TelegramSendResult`
- `TelegramUpdate`, `TelegramIncomingMessage`, `TelegramCallbackQuery`, `TelegramMedia`,
  `TelegramChat`, `TelegramSender`
- `TelegramException`
- `TelegramText`, `TelegramWebhook`, `TelegramCommand`

## Configuration

See [configuration.md](./configuration.md).
