# Messaging Configuration

Only broker transports read configuration; the in-process bus needs none.

The split is the same in every provider: **connection settings come from configuration**, because
host and credentials differ per environment and must not be compiled in. **Subscriptions and
assemblies stay in code**, because a subscription is a compile-time relationship between a handler
and an event — moving it into `appsettings` turns a build error into a queue that silently never
fills.

## `TarsRabbitMqOptions`

```json
"Tars": {
  "Messaging": {
    "RabbitMq": {
      "Host": "localhost",
      "Port": 5672,
      "VirtualHost": "/",
      "Username": "guest",
      "Password": "guest",
      "UseSsl": false,
      "RoutedExchangeType": "topic",
      "RetryLimit": 3,
      "RetryInterval": "00:00:05",
      "PrefetchCount": 16,
      "Messaging": { "EndpointName": "my-service" }
    }
  }
}
```

- `Host` / `Port` / `VirtualHost`: broker coordinates. Default `localhost:5672` on `/`
- `Username` / `Password`: credentials. Default `guest`/`guest` — supply the real ones through an
  environment variable or a mounted secret, not a committed file
- `UseSsl`: default `false`
- `RoutedExchangeType`: exchange type for events that carry a routing key — `topic` (default, allows
  wildcard bindings), `direct` (exact match), or `headers`. Broadcast events ignore it and use a
  fanout exchange, because they have no key to match. **`headers` has no Kafka equivalent**, so an
  application using it cannot later move to Kafka unchanged
- `RetryLimit` / `RetryInterval`: attempts before a message goes to the error queue. Defaults `3`
  and `5` seconds
- `PrefetchCount`: messages a consumer may hold at once. Default `16`; use `1` for long work so one
  slow message does not stack up behind it
- `Messaging:EndpointName`: the queue. Default `tars`

## `TarsKafkaOptions`

```json
"Tars": {
  "Messaging": {
    "Kafka": {
      "BootstrapServers": "localhost:9092",
      "ConsumerGroup": "my-service",
      "AutoOffsetReset": "Earliest",
      "ConcurrentMessageLimit": 16,
      "Messaging": { "EndpointName": "my-service" }
    }
  }
}
```

- `BootstrapServers`: comma-separated broker list. Default `localhost:9092`
- `ConsumerGroup`: defaults to `Messaging:EndpointName`, so one portable name means "queue" on
  RabbitMQ and "consumer group" here
- `AutoOffsetReset`: `Earliest` (default) or `Latest` — where a brand-new consumer group starts
  reading
- `ConcurrentMessageLimit`: default `16`; use `1` for long work

## `BrokerMessagingOptions`

```json
"Tars": { "Messaging": { "Broker": { "EndpointName": "my-service" } } }
```

Only `EndpointName` is bindable, and in practice each provider carries its own copy nested under its
section (`Tars:Messaging:RabbitMq:Messaging:EndpointName`). Binding this section on its own is only
needed when composing the broker core by hand.

## Binding

Each provider follows the same shape as the rest of the framework — an options binder plus a
registration call:

```csharp
builder.AddTarsRabbitMqOptions();          // binds Tars:Messaging:RabbitMq into IOptions<>
builder.AddTarsKafkaOptions();             // binds Tars:Messaging:Kafka
builder.AddTarsBrokerMessagingOptions();   // binds Tars:Messaging:Broker
```

All three accept a custom section name and a post-bind callback:

```csharp
builder.AddTarsRabbitMqOptions(
    sectionName: "MyApp:Broker",
    configure: o => o.PrefetchCount = 1);
```

### Registering the provider from configuration

The overload most applications want reads the section and registers everything in one call:

```csharp
builder.AddTarsMassTransitRabbitMq(configure: o =>
{
    // host, port and credentials already came from Tars:Messaging:RabbitMq
    o.Messaging.RegisterEventsFromAssembly(typeof(Events).Assembly);
    o.Messaging.RegisterHandlersFromAssembly(typeof(Handlers).Assembly);
    o.Messaging.Subscribe<InboundInteractionReceived>("agenda.#");
});
```

The code-only overload stays available when nothing comes from configuration:

```csharp
services.AddTarsMassTransitRabbitMq(o => { o.Host = "localhost"; /* ... */ });
```

> Configuration is read **eagerly, at registration**, not deferred through `IOptions`. The broker
> topology — exchanges, queues, bindings, Kafka producers — is built while services are being
> registered, before any provider exists to resolve options from. A change to these values therefore
> needs a restart, and configuration is not a way around the capability guard: a routed subscription
> on Kafka still fails at startup.

## Local development

`docker-compose` with the management UI on 15672:

```yaml
rabbitmq:
  image: rabbitmq:3-management
  ports: ["5672:5672", "15672:15672"]
```

Defaults already point at `localhost:5672` with `guest`/`guest`, so no configuration is needed to
get started.

## Notes

- The bot token equivalent applies here too: **`Password` and `BootstrapServers` credentials belong
  in environment variables** (`Tars__Messaging__RabbitMq__Password`) or a mounted secret.
- `RetryLimit`, `RetryInterval` and `PrefetchCount` configure MassTransit; the framework owns the
  behaviour, this only exposes it.
