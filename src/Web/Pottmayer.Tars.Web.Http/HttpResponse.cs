using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http;

public sealed class HttpResponse<T> : IHttpResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? TraceId { get; init; }
}
