namespace Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;

/// <summary>
/// Represents the next delegate in the request pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <returns>The response from the next handler in the pipeline.</returns>
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();
