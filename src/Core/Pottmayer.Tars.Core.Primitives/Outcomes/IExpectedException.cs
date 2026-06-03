namespace Pottmayer.Tars.Core.Primitives.Outcomes;

/// <summary>
/// Marker for exceptions that represent expected (domain/validation) failures
/// and should be mapped to <see cref="Result{T}"/> failures instead of being rethrown.
/// </summary>
public interface IExpectedException
{
    /// <summary>
    /// The errors to return in the failed result.
    /// </summary>
    IReadOnlyList<Error> Errors { get; }
}
