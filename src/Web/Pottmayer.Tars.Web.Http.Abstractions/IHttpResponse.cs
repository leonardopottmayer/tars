namespace Pottmayer.Tars.Web.Http.Abstractions;

/// <summary>
/// Represents the common fields of an HTTP response envelope.
/// </summary>
public interface IHttpResponse
{
    /// <summary>Gets whether the response represents a successful operation.</summary>
    bool Success { get; }
}

/// <summary>
/// Represents a successful HTTP response envelope with data.
/// </summary>
/// <typeparam name="T">The type of response data.</typeparam>
public interface IHttpResponse<T> : IHttpResponse
{
    /// <summary>Gets the response data.</summary>
    T? Data { get; }
}
