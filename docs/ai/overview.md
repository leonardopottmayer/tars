# AI Overview

## Projects in this family

- `Pottmayer.Tars.Ai.Abstractions`
- `Pottmayer.Tars.Ai.Chat.Abstractions`
- `Pottmayer.Tars.Ai.Chat`
- `Pottmayer.Tars.Ai.Chat.Gemini`

## What the module offers

A transport for chat-completion models that support **tool calling**, behind one contract, with the
provider chosen per call:

- an `IAiChatCompletionClient` contract: send a `ChatRequest`, get a `ChatCompletion` back — prose, or
  a set of tool calls the model chose
- an `IAiChatCompletionClientFactory` that resolves the client for a named provider, so one application
  can host several providers at once and pick per call (typically from the user's profile)
- a shared message/tool model (`ChatMessage`, `ToolDefinition`, `ToolCall`, `TokenUsage`)
- an `AiException` that says whether a failure is **permanent** (retrying cannot help) or **transient**
- a Gemini provider over the Generative Language API's `generateContent`, with per-request API keys

Like Communication, the module is **transport only** — it does not queue, retry, persist conversation
history, or hold prompts. Prompts, catalogs, validation and persistence belong to the caller.

## Namespaced by capability

The family is namespaced by **capability**, not by provider: `Ai.Chat.*` is the chat-completion
capability. This leaves room for `Ai.Transcription.*` and `Ai.Embedding.*` to land later without
reorganizing what exists. `Ai.Abstractions` holds only what is shared across every capability today —
`AiException`.

| Project | Level | Contents |
|---|---|---|
| `Pottmayer.Tars.Ai.Abstractions` | Abstractions | `AiException` (permanent/transient), shared across capabilities |
| `Pottmayer.Tars.Ai.Chat.Abstractions` | Abstractions | `IAiChatCompletionClient`, `IAiChatCompletionClientFactory`, the message/tool model |
| `Pottmayer.Tars.Ai.Chat` | Runtime | `AddTarsAiClientFactory` and the keyed-DI factory |
| `Pottmayer.Tars.Ai.Chat.Gemini` | Provider | `IAiChatCompletionClient` over Gemini's `generateContent` |

## The transport seam

Callers depend only on `IAiChatCompletionClient` (or resolve one through
`IAiChatCompletionClientFactory`). Which provider backs a call is a **composition choice**: each
provider package registers its client under a **service key equal to its provider name**
(`gemini`, …) via keyed DI, and the factory looks it up by that key. Adding a provider is a
registration, not a change to the factory or to callers.

```csharp
public sealed class Interpreter(IAiChatCompletionClientFactory factory)
{
    public Task<ChatCompletion> RunAsync(string provider, ChatRequest request, CancellationToken ct)
        => factory.GetClient(provider).CompleteAsync(request, ct);
}
```

`GetClient` throws a **permanent** `AiException` when no provider by that name is registered.

## Model and key are per call

`ChatRequest` carries the `Model` and (optionally) the `ApiKey` on **each request**, not as
application-wide defaults:

- **Model per call** — the same client instance serves whatever model the request names, so switching
  model is a field on the request, not a re-registration.
- **Key per call** — when `ChatRequest.ApiKey` is set it overrides whatever key the provider was
  configured with. This is the "each end user brings their own key" case: a multi-user host fetches the
  caller's key from its own credential store and passes it on the request; the provider needs no default
  key. When the request carries none, the provider falls back to its configured key.

`ChatRequest.ToString()` masks the key, so it never lands in logs.

Pass `Temperature: 0` for the deterministic output a command pipeline wants.

## Failure model

Provider calls fail with `AiException`. The one field callers act on is `IsPermanent`:

- **permanent** — the same call will fail identically on a retry (unknown model, malformed request,
  missing/invalid key, a tool's `ParametersJsonSchema` that is not valid JSON). Stop and record it.
- **transient** — endpoint unreachable, a timeout, a malformed or empty response. A retry may succeed.

`AiException` also carries `Provider`, `Model` and, when the failure came from the server, `StatusCode`.
`AiException` lives in `Ai.Abstractions` because it is shared by every capability, not just chat.

## Minimal registration

Register the factory once, then each provider you want. For Gemini:

```csharp
builder.AddTarsAiChatGeminiOptions();              // binds Tars:Ai:Chat:Gemini
builder.Services.AddTarsAiChatGeminiHttpClient();  // the typed HttpClient
builder.Services.AddTarsAiChatCompletionClientGemini(); // keyed "gemini"
builder.Services.AddTarsAiClientFactory();         // the provider factory
```

`AddTarsAiClientFactory` registers the factory with `TryAddSingleton`, so it is idempotent — call it
once alongside your provider registrations. See [configuration.md](./configuration.md).

## Making a chat call with tools

```csharp
var request = new ChatRequest(
    Model: "gemini-3.6-flash",
    Messages:
    [
        ChatMessage.System("You turn requests into a single tool call. Timestamps are ISO-8601."),
        ChatMessage.User("me lembra de ligar pro dentista amanhã às 9"),
    ],
    Tools:
    [
        new ToolDefinition(
            Name: "create_reminder",
            Description: "Creates a reminder.",
            ParametersJsonSchema: """
            { "type": "object",
              "properties": {
                "title":    { "type": "string" },
                "remindAt": { "type": "string", "description": "ISO-8601" }
              },
              "required": ["title", "remindAt"] }
            """),
    ],
    Temperature: 0,
    ApiKey: usersGeminiKey);

var completion = await client.CompleteAsync(request, ct);

foreach (var call in completion.ToolCalls)
{
    // call.Name == "create_reminder"; call.Arguments is a JsonElement the caller validates and executes.
}
```

The model's meaningful output is `ChatCompletion.ToolCalls` (empty when it replied in prose;
`completion.Message.Content` holds the text). The model never invokes anything itself — the caller
validates `ToolCall.Arguments` against the tool's schema and decides whether to execute.

## Main contracts

- `IAiChatCompletionClient`, `IAiChatCompletionClientFactory`
- `ChatRequest`, `ChatCompletion`, `ChatMessage`, `ChatRole`
- `ToolDefinition`, `ToolCall`, `TokenUsage`
- `AiException`

## Configuration

See [configuration.md](./configuration.md).

## Scenarios and testing

See [scenarios.md](./scenarios.md) for single-tenant vs per-user-key setups, hosting several providers,
and how to fake `IAiChatCompletionClient` / the factory in tests.
