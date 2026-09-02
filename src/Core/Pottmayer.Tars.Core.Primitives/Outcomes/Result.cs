namespace Pottmayer.Tars.Core.Primitives.Outcomes
{
    /// <summary>
    /// Outcome of an operation that either succeeds or fails with one or more <see cref="Error"/>s. A success
    /// carries no errors; a failure carries at least one.
    /// </summary>
    public class Result
    {
        /// <summary>Whether the operation succeeded.</summary>
        public bool IsSuccess { get; }

        /// <summary>Whether the operation failed. The inverse of <see cref="IsSuccess"/>.</summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>The errors describing a failure; empty on success.</summary>
        public IReadOnlyList<Error> Errors { get; }

        /// <summary>Optional correlation id carried alongside the outcome.</summary>
        public string? CorrelationId { get; }

        /// <summary>Initializes a result, enforcing the success/errors invariants.</summary>
        /// <param name="isSuccess">Whether the result represents success.</param>
        /// <param name="errors">The errors for a failure; must be empty on success.</param>
        /// <param name="correlationId">Optional correlation id.</param>
        /// <exception cref="InvalidOperationException">A success carries errors, or a failure carries none.</exception>
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

        /// <summary>Creates a successful result.</summary>
        /// <param name="correlationId">Optional correlation id.</param>
        /// <returns>A successful <see cref="Result"/>.</returns>
        public static Result Success(string? correlationId = null)
            => new(true, Array.Empty<Error>(), correlationId);

        /// <summary>Creates a failed result from the given errors.</summary>
        /// <param name="errors">The errors describing the failure.</param>
        /// <returns>A failed <see cref="Result"/>.</returns>
        public static Result Failure(params Error[] errors)
            => new(false, errors?.ToList() ?? new List<Error>());

        /// <summary>Creates a failed result from the given errors.</summary>
        /// <param name="errors">The errors describing the failure.</param>
        /// <param name="correlationId">Optional correlation id.</param>
        /// <returns>A failed <see cref="Result"/>.</returns>
        public static Result Failure(IEnumerable<Error> errors, string? correlationId = null)
            => new(false, errors.ToList(), correlationId);
    }

    /// <summary>
    /// Outcome of an operation that yields a value of type <typeparamref name="T"/> on success, or fails with
    /// one or more <see cref="Error"/>s. A success always carries a non-null value.
    /// </summary>
    /// <typeparam name="T">The type of the value produced on success.</typeparam>
    public sealed class Result<T> : Result
        where T : notnull
    {
        /// <summary>The value produced on success; <c>null</c> on failure.</summary>
        public T? Value { get; }

        /// <summary>Initializes a typed result, enforcing that a success carries a non-null value.</summary>
        /// <param name="isSuccess">Whether the result represents success.</param>
        /// <param name="value">The value for a success; ignored on failure.</param>
        /// <param name="errors">The errors for a failure.</param>
        /// <param name="correlationId">Optional correlation id.</param>
        /// <exception cref="InvalidOperationException">A success carries a null value.</exception>
        private Result(bool isSuccess, T? value, IReadOnlyList<Error>? errors, string? correlationId)
            : base(isSuccess, errors, correlationId)
        {
            Value = value;

            if (IsSuccess && Value is null)
                throw new InvalidOperationException("A successful result must contain a non-null value.");
        }

        /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
        /// <param name="value">The value produced.</param>
        /// <param name="correlationId">Optional correlation id.</param>
        /// <returns>A successful <see cref="Result{T}"/>.</returns>
        public static Result<T> Success(T value, string? correlationId = null)
            => new(true, value, Array.Empty<Error>(), correlationId);

        /// <summary>Creates a failed result from the given errors.</summary>
        /// <param name="errors">The errors describing the failure.</param>
        /// <returns>A failed <see cref="Result{T}"/>.</returns>
        public static new Result<T> Failure(params Error[] errors)
            => new(false, default, errors?.ToList() ?? new List<Error>(), correlationId: null);

        /// <summary>Creates a failed result from the given errors.</summary>
        /// <param name="errors">The errors describing the failure.</param>
        /// <param name="correlationId">Optional correlation id.</param>
        /// <returns>A failed <see cref="Result{T}"/>.</returns>
        public static new Result<T> Failure(IEnumerable<Error> errors, string? correlationId = null)
            => new(false, default, errors.ToList(), correlationId);

        /// <summary>Implicitly wraps a single <see cref="Error"/> into a failed result.</summary>
        /// <param name="error">The error to wrap.</param>
        public static implicit operator Result<T>(Error error) => Failure(error);
    }
}
