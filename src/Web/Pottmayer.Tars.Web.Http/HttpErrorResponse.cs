using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http;

public sealed class HttpErrorResponse : IHttpErrorResponse
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<IHttpFieldError>? FieldErrors { get; init; }
    public string? TraceId { get; init; }
}
