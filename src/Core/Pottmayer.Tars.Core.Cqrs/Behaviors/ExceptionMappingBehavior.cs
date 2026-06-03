using Pottmayer.Tars.Core.Cqrs.DI;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using System.Reflection;

namespace Pottmayer.Tars.Core.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that maps <see cref="IExpectedException"/> and optionally custom-mapped exceptions
/// to <see cref="Result{T}"/> failures. Unexpected exceptions are rethrown for global middleware/logging.
/// Register via <see cref="CqrsBehaviorsDI.AddTarsCqrsExceptionMappingBehavior"/>.
/// </summary>
public sealed class ExceptionMappingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly Func<IReadOnlyList<Error>, TResponse> _createFailure = BuildFailureFactory();

    private readonly ExceptionMappingConfiguration? _configuration;

    /// <summary>
    /// Creates a new <see cref="ExceptionMappingBehavior{TRequest,TResponse}"/> instance.
    /// </summary>
    /// <param name="configuration">Optional configuration for custom exception mapping. Injected via DI.</param>
    public ExceptionMappingBehavior(ExceptionMappingConfiguration? configuration = null)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (Exception ex) when (TryMap(ex, out var errors))
        {
            return _createFailure(errors!);
        }
    }

    private bool TryMap(Exception ex, out IReadOnlyList<Error>? errors)
    {
        if (ex is IExpectedException expected)
        {
            errors = expected.Errors;
            return true;
        }

        if (_configuration?.CustomMapper is not null)
        {
            errors = _configuration.CustomMapper(ex);
            if (errors is not null && errors.Count > 0)
                return true;
        }

        errors = null;
        return false;
    }

    private static Func<IReadOnlyList<Error>, TResponse> BuildFailureFactory()
    {
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var innerType = responseType.GetGenericArguments()[0];
            var method = typeof(Result<>)
                .MakeGenericType(innerType)
                .GetMethod(nameof(Result.Failure), [typeof(IEnumerable<Error>), typeof(string)])!;
            return errors => (TResponse)method.Invoke(null, [errors, null])!;
        }

        return errors => (TResponse)(object)Result.Failure(errors);
    }
}
