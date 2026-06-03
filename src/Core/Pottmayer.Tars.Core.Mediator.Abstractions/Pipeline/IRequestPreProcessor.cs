using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;

namespace Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;

/// <summary>
/// Defines a pre-processor that runs before the request handler (e.g. validation, logging).
/// Pre-processors do not wrap the handler; they run in sequence before the pipeline executes.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
public interface IRequestPreProcessor<in TRequest>
    where TRequest : IRequest
{
    /// <summary>
    /// Processes the request before the handler is invoked.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask Process(TRequest request, CancellationToken cancellationToken = default);
}
