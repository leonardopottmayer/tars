namespace Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;

/// <summary>
/// Non-generic marker for a request. Used by pre-processors that don't need the response type.
/// </summary>
public interface IRequest;

/// <summary>
/// Marker interface for a request (command/query) that produces a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequest<out TResponse> : IRequest;
