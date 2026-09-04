# AI Configuration

Only the provider packages read configuration. `Ai.Chat` (the factory) and the abstractions need none.

## `GeminiAiOptions`

Section name:

```json
"Tars": {
  "Ai": {
    "Chat": {
      "Gemini": {
        "ApiKey": "",
        "BaseUrl": "https://generativelanguage.googleapis.com/",
        "RequestTimeout": "00:01:40"
      }
    }
  }
}
```

Fields:

- `ApiKey`: the Google AI Studio API key used as the **default** when a request carries none. Optional —
  a host that passes each user's key per request (`ChatRequest.ApiKey`) needs no default. When set, the
  key is sent in the `x-goog-api-key` header, never in the URL
- `BaseUrl`: the API root. Default: `https://generativelanguage.googleapis.com/`. Must be an absolute
  URL
- `RequestTimeout`: the per-request deadline on the underlying `HttpClient`. Default: `100` seconds. A
  cloud model answers in seconds, so a bounded ceiling is right here (unlike a local model)

Validation runs on application start (`ValidateOnStart`): `BaseUrl` must be an absolute URI and
`RequestTimeout` must be strictly positive. The API key is **not** validated — it may be supplied per
request.

## Binding

`AddTarsAiChatGeminiOptions` binds the options from configuration:

```csharp
builder.AddTarsAiChatGeminiOptions();
```

It accepts a custom section name and a post-bind callback:

```csharp
builder.AddTarsAiChatGeminiOptions(
    sectionName: "MyApp:Gemini",
    configure: o => o.RequestTimeout = TimeSpan.FromSeconds(60));
```

- `sectionName`: overrides the default `Tars:Ai:Chat:Gemini`
- `configure`: runs after binding, so it overrides values read from configuration

## Full registration

The Gemini provider is three registrations plus the shared factory:

```csharp
builder.AddTarsAiChatGeminiOptions();               // 1. binds + validates GeminiAiOptions
builder.Services.AddTarsAiChatGeminiHttpClient();   // 2. typed HttpClient (base address + timeout)
builder.Services.AddTarsAiChatCompletionClientGemini(); // 3. keyed IAiChatCompletionClient ("gemini")
builder.Services.AddTarsAiClientFactory();          // 4. IAiChatCompletionClientFactory
```

Order and dependencies:

- `AddTarsAiChatGeminiHttpClient` reads `GeminiAiOptions`, so it needs (1). It configures the typed
  `HttpClient`'s base address and timeout; the API key is **not** a client default header — it is applied
  per request
- `AddTarsAiChatCompletionClientGemini` registers `GeminiAiChatCompletionClient` as the
  `IAiChatCompletionClient` **keyed** by its provider name (`gemini`), via `TryAdd`. It needs the typed
  client (2) and the factory (4)
- `AddTarsAiClientFactory` registers the factory with `TryAddSingleton` — idempotent; call it once even
  with several providers

## The per-user key pattern

The intended multi-user setup keeps **no** default key in configuration and passes each user's key on
the request:

```csharp
// Leave GeminiAiOptions.ApiKey empty. Fetch the caller's key from your own credential store,
// then pass it on the request:
var request = new ChatRequest(model, messages, tools, Temperature: 0, ApiKey: usersGeminiKey);
var completion = await factory.GetClient("gemini").CompleteAsync(request, ct);
```

If neither the request nor the options carry a key, the provider throws a **permanent** `AiException`
("No Gemini API key…") rather than reaching the endpoint with an unusable request.

## Keeping the key out of configuration files

The Gemini API key is a credential. When you do use the options default (a single-tenant service),
supply it through an environment variable (`Tars__Ai__Chat__Gemini__ApiKey`) or a mounted secret, not a
committed `appsettings.json`. In a multi-user host, prefer the per-request key above so no shared key
exists at all.

## Notes

- The factory resolves providers by keyed DI: `GetClient("gemini")` returns the client registered under
  the key `gemini`. Asking for a provider that was never registered throws a permanent `AiException`.
- Adding another provider (e.g. OpenAI) is a new `Ai.Chat.<Provider>` package that registers its client
  under its own key — no change to the factory or to callers.
