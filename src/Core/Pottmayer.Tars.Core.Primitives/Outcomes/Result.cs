namespace Pottmayer.Tars.Core.Primitives.Outcomes
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        public IReadOnlyList<Error> Errors { get; }
        public string? CorrelationId { get; }

        protected Result(bool isSuccess, IReadOnlyList<Error>? errors = null, string? correlationId = null)
        {
            IsSuccess = isSuccess;
            Errors = errors ?? Array.Empty<Error>();
            CorrelationId = correlationId;

            if (IsSuccess && Errors.Count > 0)
                throw new InvalidOperationException("A successful result cannot contain errors.");

            if (!IsSuccess && Errors.Count == 0)
                throw new InvalidOperationException("A failure result must contain at least one error.");
        }

        public static Result Success(string? correlationId = null)
            => new(true, Array.Empty<Error>(), correlationId);

        public static Result Failure(params Error[] errors)
            => new(false, errors?.ToList() ?? new List<Error>());

        public static Result Failure(IEnumerable<Error> errors, string? correlationId = null)
            => new(false, errors.ToList(), correlationId);
    }

    public sealed class Result<T> : Result
        where T : notnull
    {
        public T? Value { get; }

        private Result(bool isSuccess, T? value, IReadOnlyList<Error>? errors, string? correlationId)
            : base(isSuccess, errors, correlationId)
        {
            Value = value;

            if (IsSuccess && Value is null)
                throw new InvalidOperationException("A successful result must contain a non-null value.");
        }

        public static Result<T> Success(T value, string? correlationId = null)
            => new(true, value, Array.Empty<Error>(), correlationId);

        public static new Result<T> Failure(params Error[] errors)
            => new(false, default, errors?.ToList() ?? new List<Error>(), correlationId: null);

        public static new Result<T> Failure(IEnumerable<Error> errors, string? correlationId = null)
            => new(false, default, errors.ToList(), correlationId);

        public static implicit operator Result<T>(Error error) => Failure(error);
    }
}
