using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.Security.Authorization.Abstractions;

namespace Pottmayer.Tars.Security.Authorization.Behaviors;

/// <summary>
/// Runs <see cref="AuthorizationGate"/> for requests marked with <see cref="RequiresPermissionAttribute"/>
/// and answers a refusal with a failed <see cref="Result"/>, so HTTP, bots and jobs see the same
/// outcome. Unmarked requests pass straight through.
/// </summary>
internal sealed class AuthorizationBehavior<TRequest, TResponse>(IServiceProvider services) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly string[] _permissions = typeof(TRequest)
        .GetCustomAttributes<RequiresPermissionAttribute>(inherit: true)
        .Select(a => a.Permission)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    private static readonly Func<Error, TResponse> _createFailure = BuildFailureFactory();

    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        if (_permissions.Length == 0)
            return await next();

        // Resolved only here, so unmarked requests work even where authorization is not wired.
        var actor = services.GetService<IAuthorizationActorResolver>() ?? throw new InvalidOperationException(
            $"{typeof(TRequest).Name} requires a permission, but no IAuthorizationActorResolver is registered. Call AddTarsAuthorizationActorResolver.");
        var gate = services.GetRequiredService<AuthorizationGate>();

        var error = await gate.CheckAsync(actor.Resolve(request), _permissions, cancellationToken);
        return error is null ? await next() : _createFailure(error);
    }

    // Same shape as the CQRS exception-mapping behavior: Result<T> needs its own Failure overload.
    private static Func<Error, TResponse> BuildFailureFactory()
    {
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var method = responseType.GetMethod(nameof(Result.Failure), [typeof(IEnumerable<Error>), typeof(string)])!;
            return error => (TResponse)method.Invoke(null, [new[] { error }, null])!;
        }

        return error => (TResponse)(object)Result.Failure(error);
    }
}
