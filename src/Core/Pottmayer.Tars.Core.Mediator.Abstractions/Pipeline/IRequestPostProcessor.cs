using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;

namespace Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;

/// <summary>
/// Defines a post-processor that runs after the request handler (e.g. logging response, cleanup).
/// Post-processors run in sequence after the handler completes.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequestPostProcessor<in TRequest, in TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Processes the request and response after the handler has completed.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="response">The response from the handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask Process(TRequest request, TResponse response, CancellationToken cancellationToken = default);
}
