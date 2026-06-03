using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;

namespace Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;

/// <summary>
/// Defines a pipeline behavior that wraps the execution of a request handler.
/// Behaviors are executed in the order they are registered (first registered = outermost).
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request by optionally wrapping the call to the next delegate.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response.</returns>
    ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default);
}
