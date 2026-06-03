namespace Pottmayer.Tars.Web.Http.Abstractions;

public interface IHttpErrorResponse : IHttpResponse
{
    string? ErrorCode { get; }
    string? ErrorMessage { get; }
    IReadOnlyList<IHttpFieldError>? FieldErrors { get; }
}
