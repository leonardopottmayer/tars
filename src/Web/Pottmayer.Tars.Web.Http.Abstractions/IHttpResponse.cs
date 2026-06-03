namespace Pottmayer.Tars.Web.Http.Abstractions;

public interface IHttpResponse
{
    bool Success { get; }
}

public interface IHttpResponse<T> : IHttpResponse
{
    T? Data { get; }
}
