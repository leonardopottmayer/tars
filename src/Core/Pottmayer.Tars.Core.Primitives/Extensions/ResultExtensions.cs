using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Primitives.Extensions
{
    public static class ResultExtensions
    {
        public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map)
            where TIn : notnull
            where TOut : notnull
        {
            if (result.IsFailure)
                return Result<TOut>.Failure(result.Errors, result.CorrelationId);

            return Result<TOut>.Success(map(result.Value!), result.CorrelationId);
        }

        public static Result<T> WithCorrelationId<T>(this Result<T> result, string correlationId)
            where T : notnull
        {
            return result.IsSuccess
                ? Result<T>.Success(result.Value!, correlationId)
                : Result<T>.Failure(result.Errors, correlationId);
        }

        public static Result WithCorrelationId(this Result result, string correlationId)
        {
            return result.IsSuccess
                ? Result.Success(correlationId)
                : Result.Failure(result.Errors, correlationId);
        }

        public static Error? FirstErrorOrNull(this Result result)
            => result.Errors.FirstOrDefault();
    }
}
