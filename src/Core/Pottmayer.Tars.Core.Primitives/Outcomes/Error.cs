namespace Pottmayer.Tars.Core.Primitives.Outcomes
{
    public sealed record Error(
        string Code,
        string Message,
        ErrorType Type = ErrorType.Unexpected,
        IReadOnlyDictionary<string, object?>? Metadata = null
    )
    {
        public static Error Validation(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Validation, metadata);

        public static Error Business(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Business, metadata);

        public static Error NotFound(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.NotFound, metadata);

        public static Error Conflict(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Conflict, metadata);

        public static Error Unauthorized(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Unauthorized, metadata);

        public static Error Forbidden(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Forbidden, metadata);

        public static Error Unexpected(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
            => new(code, message, ErrorType.Unexpected, metadata);
    }
}
