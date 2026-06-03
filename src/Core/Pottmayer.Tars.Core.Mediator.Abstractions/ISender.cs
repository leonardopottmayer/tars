using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;

namespace Pottmayer.Tars.Core.Mediator.Abstractions;

/// <summary>
/// Sends a request (command/query) and returns a single response.
/// Request flows through pre-processors, pipeline behaviors, handler, then post-processors.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request and returns the response.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the handler.</returns>
    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
