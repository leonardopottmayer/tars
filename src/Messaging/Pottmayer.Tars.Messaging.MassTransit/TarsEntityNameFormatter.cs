using MassTransit;
using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.MassTransit;

/// <summary>
/// Names broker entities after the event's logical name instead of its .NET type.
/// </summary>
/// <remarks>
/// This is what makes the wire contract survive a refactor. MassTransit's default formatter derives
/// the exchange name from the namespace and class name, so moving a record to another namespace
/// would quietly repoint it at a new exchange and existing consumers would go silent. Naming by
/// <see cref="IntegrationEventNameAttribute"/> keeps the route owned by the contract.
/// </remarks>
public sealed class TarsEntityNameFormatter : IEntityNameFormatter
{
    public string FormatEntityName<TMessage>() => IntegrationEventNaming.For(typeof(TMessage));
}
