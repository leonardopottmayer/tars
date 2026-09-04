# AI — Registration Scenarios and Testing

Each scenario shows the complete registration. See [overview.md](./overview.md) for the contracts and
[configuration.md](./configuration.md) for the full options reference.

---

## Scenario 1 — Gemini with one service-wide key (single-tenant)

The whole service uses one key, read from configuration. Requests carry no key of their own; the
provider falls back to `GeminiAiOptions.ApiKey`.

```csharp
// Program.cs
builder.AddTarsAiChatGeminiOptions();
builder.Services.AddTarsAiChatGeminiHttpClient();
builder.Services.AddTarsAiChatCompletionClientGemini();
builder.Services.AddTarsAiClientFactory();
```

```json
// appsettings.json
{
  "Tars": {
    "Ai": {
      "Chat": {
        "Gemini": {
          "ApiKey": "<from environment/secret, not committed>"
        }
      }
    }
  }
}
```

```csharp
public sealed class Summarizer(IAiChatCompletionClientFactory factory)
{
    public async Task<string?> SummarizeAsync(string text, CancellationToken ct)
    {
        var request = new ChatRequest(
            Model: "gemini-3.6-flash",
            Messages: [ChatMessage.System("Summarize in one sentence."), ChatMessage.User(text)],
            Temperature: 0);
        var completion = await factory.GetClient("gemini").CompleteAsync(request, ct);
        return completion.Message.Content;
    }
}
```

Supply the key through `Tars__Ai__Chat__Gemini__ApiKey` (environment variable) rather than a committed
file.

---

## Scenario 2 — Gemini with per-user keys (multi-tenant)

Each end user brings their own key. **No** default key is configured; the caller fetches the user's key
from its own credential store and passes it on the request. Registration is identical to Scenario 1
minus the `ApiKey` in configuration.

```csharp
// Program.cs — same four registrations, no ApiKey in appsettings
builder.AddTarsAiChatGeminiOptions();
builder.Services.AddTarsAiChatGeminiHttpClient();
builder.Services.AddTarsAiChatCompletionClientGemini();
builder.Services.AddTarsAiClientFactory();
```

```csharp
public sealed class Interpreter(
    IAiChatCompletionClientFactory factory,
    IUserAiCredentials credentials) // your own store; not part of this family
{
    public async Task<ChatCompletion> RunAsync(Guid userId, ChatRequest baseRequest, CancellationToken ct)
    {
        var key = await credentials.GetKeyAsync(userId, "gemini", ct);
        var request = baseRequest with { ApiKey = key };   // per-call key overrides the (absent) default
        return await factory.GetClient("gemini").CompleteAsync(request, ct);
    }
}
```

When neither the request nor the options carry a key, the provider throws a **permanent** `AiException`
before reaching the endpoint. (This is exactly how Pandora's Assistant module works: the key lives in its
Integrations module and is passed per call.)

---

## Scenario 3 — Several providers, one host

Provider selection is by name, so a host can register more than one and pick per call — typically from
the user's profile. Each provider registers its client under its own key; the factory (registered once)
resolves whichever the caller asks for.

```csharp
// Program.cs
builder.AddTarsAiChatGeminiOptions();
builder.Services.AddTarsAiChatGeminiHttpClient();
builder.Services.AddTarsAiChatCompletionClientGemini();   // keyed "gemini"

// builder.Services.AddTarsAiChatCompletionClientOpenAi(); // keyed "openai", when that package exists

builder.Services.AddTarsAiClientFactory();                // resolves both
```

```csharp
// The provider name comes from the user's profile, not hard-coded.
var client = factory.GetClient(userProfile.Provider);     // "gemini" | "openai" | …
var completion = await client.CompleteAsync(request with { Model = userProfile.Model }, ct);
```

`GetClient` throws a permanent `AiException` when the named provider was never registered — a
configuration mistake, surfaced as a permanent failure rather than a silent fallback.

---

## Testing — faking `IAiChatCompletionClient`

There is no built-in fake. `IAiChatCompletionClient` is a one-method interface, so a recording fake that
returns a canned completion — or throws a chosen `AiException` — covers most application-level tests
(prompt assembly, tool-call validation, failure classification):

```csharp
internal sealed class FakeChatClient(ChatCompletion? reply = null, AiException? toThrow = null)
    : IAiChatCompletionClient
{
    public ChatRequest? LastRequest { get; private set; }

    public Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken ct = default)
    {
        LastRequest = request;                 // assert the model/key/tools the caller sent
        if (toThrow is not null) throw toThrow;
        return Task.FromResult(reply ?? new ChatCompletion(
            request.Model,
            new ChatMessage(ChatRole.Assistant, "ok"),
            new TokenUsage(0, 0)));
    }
}
```

Pair it with a factory fake when the code under test resolves through the factory:

```csharp
internal sealed class FakeChatClientFactory(IAiChatCompletionClient client) : IAiChatCompletionClientFactory
{
    public IAiChatCompletionClient GetClient(string provider) => client;
}
```

To assert **permanent vs transient** handling, hand the fake an `AiException` with the `isPermanent` you
want:

```csharp
var permanent = new FakeChatClient(toThrow: new AiException("gemini", "bad key", isPermanent: true));
var transient = new FakeChatClient(toThrow: new AiException("gemini", "endpoint down", isPermanent: false));
```

To exercise the **real** Gemini provider's wire mapping and error classification (rather than bypass it),
inject an `HttpMessageHandler` stub into the typed `HttpClient` and return canned Gemini payloads /
status codes — that is how `Pottmayer.Tars.Ai.Chat.Gemini` is unit-tested.
