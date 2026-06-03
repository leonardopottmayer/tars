using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Mediator.Abstractions.Notifications;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using System.Reflection;

namespace Pottmayer.Tars.Core.Mediator;

/// <summary>
/// Default mediator implementation that dispatches requests to handlers and publishes notifications.
/// Resolves handlers, pipeline behaviors, and processors via <see cref="IServiceProvider"/>.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly MethodInfo SendCoreMethod = typeof(Mediator).GetMethod(nameof(SendCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Creates a new <see cref="Mediator"/> instance.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers and pipeline components.</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var result = SendCoreMethod
            .MakeGenericMethod(requestType, typeof(TResponse))
            .Invoke(null, [_serviceProvider, request, cancellationToken]);

        return await (ValueTask<TResponse>)result!;
    }

    private static async ValueTask<TResponse> SendCore<TRequest, TResponse>(
        IServiceProvider serviceProvider,
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var req = (TRequest)request;

        // Pre-processors
        var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<TRequest>>();
        foreach (var preProcessor in preProcessors)
            await preProcessor.Process(req, cancellationToken).ConfigureAwait(false);

        // Handler
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        RequestHandlerDelegate<TResponse> next = () => handler.Handle(req, cancellationToken);

        // Pipeline behaviors (first registered = outermost)
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse();
        foreach (var behavior in behaviors)
        {
            var current = behavior;
            var nextCapture = next;
            next = () => current.Handle(req, nextCapture, cancellationToken);
        }

        var response = await next().ConfigureAwait(false);

        // Post-processors
        var postProcessors = serviceProvider.GetServices<IRequestPostProcessor<TRequest, TResponse>>();
        foreach (var postProcessor in postProcessors)
            await postProcessor.Process(req, response, cancellationToken).ConfigureAwait(false);

        return response;
    }

    /// <inheritdoc />
    public async ValueTask Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null)
                continue;

            var handlerConcreteType = handler.GetType();
            MethodInfo? handleMethod = handlerConcreteType.GetMethod(nameof(INotificationHandler<INotification>.Handle), [notificationType, typeof(CancellationToken)]);
            if (handleMethod is null && handlerConcreteType.GetInterfaces().Contains(handlerType))
            {
                var map = handlerConcreteType.GetInterfaceMap(handlerType);
                var idx = Array.FindIndex(map.InterfaceMethods, m => m.Name == nameof(INotificationHandler<INotification>.Handle));
                if (idx >= 0)
                    handleMethod = map.TargetMethods[idx];
            }

            if (handleMethod is null)
                continue;

            var task = (ValueTask)handleMethod.Invoke(handler, [notification, cancellationToken])!;
            await task.ConfigureAwait(false);
        }
    }
}
