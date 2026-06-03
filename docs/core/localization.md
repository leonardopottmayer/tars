# Core.Localization

## What this group solves

`Core.Localization` centralizes the resolution of textual messages for the framework and the application. The idea is simple:

- the code requests a message by key
- the `IMessageProvider` looks for that key in the registered sources
- the current culture defines which translation should be used
- if the key does not exist, the provider returns the given `fallback` or the key itself

This avoids hardcoded messages in `Web.Http`, `Identity`, application handlers, workers and notifications.

## Packages

| Package | Level | Role |
|---|---|---|
| `Pottmayer.Tars.Core.Localization.Abstractions` | Abstractions | `IMessageProvider` and `IMessageSource` |
| `Pottmayer.Tars.Core.Localization` | Runtime | `CompositeMessageProvider`, `InMemoryMessageSource`, `ResourceManagerMessageSource`, base DI |
| `Pottmayer.Tars.Core.Localization.AspNetCore` | Host Integration | `StringLocalizerMessageSource`, options and request localization middleware |

## Contracts

### `IMessageProvider`

Contract consumed by the framework and applications:

```csharp
public interface IMessageProvider
{
    string Get(string key, string? fallback = null, params object[] args);
    string Get(string key, CultureInfo culture, string? fallback = null, params object[] args);
}
```

Important rules:

- `Get(key)` uses `CultureInfo.CurrentUICulture`
- `Get(key, culture, ...)` allows forcing the culture in jobs, emails and notifications
- `args` uses `string.Format(...)`
- there is no exception for a missing key
- if nothing is found, the return is `fallback ?? key`

### `IMessageSource`

Each source knows how to look up a key for a culture:

```csharp
public interface IMessageSource
{
    string? TryGet(string key, CultureInfo culture);
}
```

`IMessageProvider` composes several sources; `IMessageSource` represents a single isolated source.

## Implementations

### `CompositeMessageProvider`

`CompositeMessageProvider` receives `IEnumerable<IMessageSource>` and queries each source in registration order:

```csharp
services.AddTarsLocalization();
services.AddTarsMessageSource(sourceA);
services.AddTarsMessageSource(sourceB);
```

In this case:

1. `sourceA` has priority
2. if `sourceA` does not resolve the key, `sourceB` is queried
3. if no source resolves it, the `fallback` kicks in

This is useful for:

- overriding framework messages from the application
- mixing in-memory and `.resx` resources
- combining the host's internal messages with messages coming from libraries

### `InMemoryMessageSource`

Good for small message sets, overrides and tests.

```csharp
var messages = new Dictionary<string, IDictionary<string, string>>
{
    ["en"] = new Dictionary<string, string>
    {
        ["app.orders.not_found"] = "Order not found.",
        ["app.orders.invalid_status"] = "Order status is invalid."
    },
    ["pt-BR"] = new Dictionary<string, string>
    {
        ["app.orders.not_found"] = "Pedido não encontrado.",
        ["app.orders.invalid_status"] = "Status do pedido inválido."
    }
};

builder.Services.AddTarsLocalization();
builder.Services.AddTarsMessageSource(new InMemoryMessageSource(messages));
```

Internal culture fallback:

1. tries the exact culture, for example `pt-BR`
2. tries the neutral culture, for example `pt`
3. tries `"en"`

### `ResourceManagerMessageSource`

Good for projects with `.resx` embedded in the assembly.

Typical structure:

```text
MyApp/
  Resources/
    AppMessages.resx
    AppMessages.pt-BR.resx
```

Registration:

```csharp
builder.Services.AddTarsLocalization();
builder.Services.AddTarsMessageSource(
    new ResourceManagerMessageSource(AppMessages.ResourceManager));
```

Usage:

```csharp
var message = _messages.Get("app.orders.not_found", fallback: "Order not found.");
```

`ResourceManagerMessageSource` delegates the culture fallback to the `ResourceManager` itself.

### `StringLocalizerMessageSource`

Good for ASP.NET Core hosts that already use `IStringLocalizer`.

```csharp
builder.AddTarsLocalizationAspNetCore();
builder.Services.AddTarsStringLocalizerSource<SharedResource>();
```

This source:

- uses `IStringLocalizerFactory`
- temporarily switches `CultureInfo.CurrentUICulture` to the requested culture
- returns `null` when the key does not exist, allowing the composite provider's fallback

## Available DI

### Base runtime

```csharp
builder.Services.AddTarsLocalization();
builder.Services.AddTarsMessageSource(new InMemoryMessageSource(messages));
builder.Services.AddTarsMessageSource(new ResourceManagerMessageSource(AppMessages.ResourceManager));
```

Methods:

| Method | Role |
|---|---|
| `AddTarsLocalization()` | registers `IMessageProvider` with `CompositeMessageProvider` |
| `AddTarsMessageSource(IMessageSource source)` | adds a source to the pipeline |

Important:

- `AddTarsLocalization()` alone does not add any messages
- if no source is registered, `Get(...)` always returns the `fallback` or the key
- if you want a fully custom provider, register your own `IMessageProvider`

### ASP.NET Core

```csharp
builder.AddTarsLocalizationAspNetCore();
builder.Services.AddTarsStringLocalizerSource<SharedResource>();

var app = builder.Build();
app.UseTarsLocalization();
```

Methods:

| Method | Role |
|---|---|
| `AddTarsLocalizationAspNetCore(...)` | registers options, `AddTarsLocalization()` and `AddLocalization()` |
| `AddTarsLocalizationAspNetCoreOptions(...)` | binds/validates options without registering the rest |
| `AddTarsStringLocalizerSource<TResource>()` | creates a source based on `IStringLocalizer` |
| `UseTarsLocalization()` | applies `RequestLocalizationMiddleware` |

## appsettings

`LocalizationAspNetCoreOptions` uses the `Tars:Localization` section:

```json
{
  "Tars": {
    "Localization": {
      "DefaultCulture": "en-US",
      "SupportedCultures": [ "en-US", "pt-BR" ]
    }
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `DefaultCulture` | `string` | `"en-US"` | the host's default culture |
| `SupportedCultures` | `string[]` | `[ "en-US" ]` | cultures accepted by the middleware |

Notes:

- with the current options classes, the most practical path is `appsettings` or section binding
- the methods accept `configure`, but the options properties are `init`; therefore, prefer file/section configuration
- `UseTarsLocalization()` configures the middleware with `DefaultCulture` and `SupportedCultures`

## Registration scenarios

### 1. Worker or console app with in-memory messages

```csharp
var services = new ServiceCollection();

services.AddTarsLocalization();
services.AddTarsMessageSource(new InMemoryMessageSource(
    new Dictionary<string, IDictionary<string, string>>
    {
        ["en"] = new Dictionary<string, string>
        {
            ["jobs.email.subject"] = "Your export is ready"
        },
        ["pt-BR"] = new Dictionary<string, string>
        {
            ["jobs.email.subject"] = "Sua exportação está pronta"
        }
    }));
```

### 2. Application with its own `.resx`

```csharp
builder.Services.AddTarsLocalization();
builder.Services.AddTarsMessageSource(
    new ResourceManagerMessageSource(AppMessages.ResourceManager));
```

Usage in a handler:

```csharp
public sealed class GetOrderQueryHandler
{
    private readonly IMessageProvider _messages;

    public GetOrderQueryHandler(IMessageProvider messages)
        => _messages = messages;

    public Task<Result<OrderDto>> Handle(GetOrderQuery query, CancellationToken ct)
    {
        var message = _messages.Get("app.orders.not_found", fallback: "Order not found.");
        return Task.FromResult(Result<OrderDto>.Failure(
            new Error("ORDER_NOT_FOUND", message, ErrorType.NotFound)));
    }
}
```

### 3. ASP.NET Core with `IStringLocalizer`

Marker class:

```csharp
namespace MyApp.Resources;

public sealed class SharedResource { }
```

Registration:

```csharp
builder.AddTarsLocalizationAspNetCore();
builder.Services.AddTarsStringLocalizerSource<MyApp.Resources.SharedResource>();

var app = builder.Build();
app.UseTarsLocalization();
```

Expected resource files:

```text
Resources/
  SharedResource.resx
  SharedResource.pt-BR.resx
```

### 4. Application override on top of the framework

```csharp
builder.Services.AddTarsLocalization();

builder.Services.AddTarsMessageSource(new InMemoryMessageSource(
    new Dictionary<string, IDictionary<string, string>>
    {
        ["pt-BR"] = new Dictionary<string, string>
        {
            ["tars.http.not_found"] = "Registro não localizado."
        }
    }));

builder.Services.AddTarsMessageSource(
    new ResourceManagerMessageSource(AppMessages.ResourceManager));
```

Since the provider queries in registration order, the first source can override framework keys.

### 5. Fully custom provider

If the messages come from a database, external API or distributed cache, you can replace the provider:

```csharp
builder.Services.AddSingleton<IMessageProvider, DatabaseMessageProvider>();
```

In this case, `AddTarsLocalization()` is no longer necessary.

## Usage in the application layer

### Domain-specific messages

```csharp
public static class OrderMessageKeys
{
    public const string NotFound = "app.orders.not_found";
    public const string CannotCancelShipped = "app.orders.cannot_cancel_shipped";
}

var message = _messages.Get(
    OrderMessageKeys.CannotCancelShipped,
    fallback: "Shipped orders cannot be cancelled.");
```

### Parameterized message

```csharp
var message = _messages.Get(
    "app.orders.already_exists",
    fallback: "Order '{0}' already exists.",
    request.Number);
```

### Worker, email or notification with an explicit culture

```csharp
var culture = CultureInfo.GetCultureInfo(user.PreferredLanguage);

var subject = _messages.Get(
    "emails.welcome.subject",
    culture,
    fallback: "Welcome to the platform!");
```

## Relationship with `Web.Http`

`Pottmayer.Tars.Web.Http.DefaultHttpErrorMapper` relies on `IMessageProvider` to build default messages such as:

- `tars.http.not_found`
- `tars.http.validation`
- `tars.http.internal_server_error`

For this reason, the minimal registration to use the default mapper is:

```csharp
builder.Services.AddTarsLocalization();
builder.Services.AddTarsDefaultHttpErrorMapper();
```

`AddTarsDefaultHttpErrorMapper()` itself registers the default Tars messages in memory, but it still requires the `IMessageProvider` to exist.

## Common pitfalls

- Registering `AddTarsLocalization()` and forgetting to add sources: in this case everything falls back to `fallback`.
- Assuming `AddTarsLocalizationAspNetCore()` registers resources automatically: it registers the host and the middleware, not your application keys.
- Using `Error.Message` with already-fixed text and expecting automatic translation in `Web.Http`: if the text is already filled in, the default mapper uses that text.
- Forgetting `app.UseTarsLocalization()` in the HTTP pipeline: the current culture will not be adjusted per request.
