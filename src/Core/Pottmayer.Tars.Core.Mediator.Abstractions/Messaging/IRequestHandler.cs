namespace Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;

/// <summary>
/// Defines a handler for a request of type <typeparamref name="TRequest"/> that returns <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request asynchronously.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response.</returns>
    ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}
