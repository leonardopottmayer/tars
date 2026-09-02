using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Primitives.Extensions
{
    /// <summary>Functional helpers for transforming and enriching <see cref="Result"/> values.</summary>
    public static class ResultExtensions
    {
        /// <summary>Projects the value of a successful result; propagates the errors of a failed one unchanged.</summary>
        /// <typeparam name="TIn">The source value type.</typeparam>
        /// <typeparam name="TOut">The projected value type.</typeparam>
        /// <param name="result">The result to map.</param>
        /// <param name="map">Projection applied to the value on success.</param>
        /// <returns>A mapped success, or the original failure.</returns>
        public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map)
            where TIn : notnull
            where TOut : notnull
        {
            if (result.IsFailure)
                return Result<TOut>.Failure(result.Errors, result.CorrelationId);

            return Result<TOut>.Success(map(result.Value!), result.CorrelationId);
        }

        /// <summary>Returns a copy of the result with the given correlation id.</summary>
        /// <typeparam name="T">The result value type.</typeparam>
        /// <param name="result">The result to enrich.</param>
        /// <param name="correlationId">The correlation id to attach.</param>
        /// <returns>An equivalent result carrying <paramref name="correlationId"/>.</returns>
        public static Result<T> WithCorrelationId<T>(this Result<T> result, string correlationId)
            where T : notnull
        {
            return result.IsSuccess
                ? Result<T>.Success(result.Value!, correlationId)
                : Result<T>.Failure(result.Errors, correlationId);
        }

        /// <summary>Returns a copy of the result with the given correlation id.</summary>
        /// <param name="result">The result to enrich.</param>
        /// <param name="correlationId">The correlation id to attach.</param>
        /// <returns>An equivalent result carrying <paramref name="correlationId"/>.</returns>
        public static Result WithCorrelationId(this Result result, string correlationId)
        {
            return result.IsSuccess
                ? Result.Success(correlationId)
                : Result.Failure(result.Errors, correlationId);
        }

        /// <summary>Returns the first error of a result, or <c>null</c> when there are none.</summary>
        /// <param name="result">The result to inspect.</param>
        /// <returns>The first <see cref="Error"/>, or <c>null</c>.</returns>
        public static Error? FirstErrorOrNull(this Result result)
            => result.Errors.FirstOrDefault();
    }
}
