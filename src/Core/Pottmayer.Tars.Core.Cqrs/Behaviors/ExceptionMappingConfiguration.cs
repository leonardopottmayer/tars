using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.Behaviors;

/// <summary>
/// Configuration for <see cref="ExceptionMappingBehavior{TRequest,TResult}"/>.
/// Allows injecting a custom exception-to-errors mapper into the generic behavior via DI.
/// </summary>
public class ExceptionMappingConfiguration
{
    /// <summary>
    /// Optional custom mapper for exceptions that don't implement <see cref="IExpectedException"/>.
    /// </summary>
    public Func<Exception, IReadOnlyList<Error>>? CustomMapper { get; set; }
}
