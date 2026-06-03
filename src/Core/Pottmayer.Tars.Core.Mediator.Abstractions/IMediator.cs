namespace Pottmayer.Tars.Core.Mediator.Abstractions;

/// <summary>
/// Mediator that supports both request/response (Send) and notification publishing (Publish).
/// </summary>
public interface IMediator : ISender, IPublisher;
