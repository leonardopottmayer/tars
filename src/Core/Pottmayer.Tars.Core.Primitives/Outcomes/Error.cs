namespace Pottmayer.Tars.Core.Primitives.Outcomes
{
    /// <summary>
    /// A single failure detail carried by a <see cref="Result"/>: a machine-readable code, a human-readable
    /// message, a <see cref="ErrorType"/> classification and optional metadata. Static factories create errors
    /// of each type.
    /// </summary>
    /// <param name="Code">Machine-readable error code.</param>
    /// <param name="Message">Human-readable error message.</param>
    /// <param name="Type">Classification of the error. Defaults to <see cref="ErrorType.Unexpected"/>.</param>
    /// <param name="Metadata">Optional additional data about the error.</param>
    public sealed record Error(
        string Code,
        string Message,
        ErrorType Type = ErrorType.Unexpected,
        IReadOnlyDictionary<string, object?>? Metadata = null
    )
    {
        /// <summary>Creates a <see cref="ErrorType.Validation"/> error.</summary>
        /// <param name="code">Machine-readable error code.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="metadata">Optional additional data about the error.</param>
        /// <returns>The created <see cref="Error"/>.</returns>
        public static Error Validation(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Validation, metadata);

        /// <summary>Creates a <see cref="ErrorType.Business"/> error.</summary>
        /// <param name="code">Machine-readable error code.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="metadata">Optional additional data about the error.</param>
        /// <returns>The created <see cref="Error"/>.</returns>
        public static Error Business(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Business, metadata);

        /// <summary>Creates a <see cref="ErrorType.NotFound"/> error.</summary>
        /// <param name="code">Machine-readable error code.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="metadata">Optional additional data about the error.</param>
        /// <returns>The created <see cref="Error"/>.</returns>
        public static Error NotFound(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.NotFound, metadata);

        /// <summary>Creates a <see cref="ErrorType.Conflict"/> error.</summary>
        /// <param name="code">Machine-readable error code.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="metadata">Optional additional data about the error.</param>
        /// <returns>The created <see cref="Error"/>.</returns>
        public static Error Conflict(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Conflict, metadata);

        /// <summary>Creates a <see cref="ErrorType.Unauthorized"/> error.</summary>
        /// <param name="code">Machine-readable error code.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="metadata">Optional additional data about the error.</param>
        /// <returns>The created <see cref="Error"/>.</returns>
        public static Error Unauthorized(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Unauthorized, metadata);

        /// <summary>Creates a <see cref="ErrorType.Forbidden"/> error.</summary>
        /// <param name="code">Machine-readable error code.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="metadata">Optional additional data about the error.</param>
        /// <returns>The created <see cref="Error"/>.</returns>
        public static Error Forbidden(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Forbidden, metadata);

        /// <summary>Creates a <see cref="ErrorType.Unexpected"/> error.</summary>
        /// <param name="code">Machine-readable error code.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="metadata">Optional additional data about the error.</param>
        /// <returns>The created <see cref="Error"/>.</returns>
        public static Error Unexpected(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Unexpected, metadata);
    }
}
