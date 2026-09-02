using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Web.Http.Abstractions;

/// <summary>
/// Maps domain errors and exceptions to HTTP error envelopes.
/// </summary>
public interface IHttpErrorMapper
{
    /// <summary>Maps an error type to an HTTP status code.</summary>
    /// <param name="errorType">The domain error type.</param>
    /// <returns>The corresponding HTTP status code.</returns>
    int MapToStatusCode(ErrorType errorType);
    /// <summary>Maps a domain error to an HTTP error response.</summary>
    /// <param name="error">The domain error.</param>
    /// <returns>The mapped HTTP error response.</returns>
    IHttpErrorResponse Map(Error error);
    /// <summary>Maps an exception to an HTTP error response.</summary>
    /// <param name="exception">The exception to map.</param>
    /// <returns>The mapped HTTP error response.</returns>
    IHttpErrorResponse Map(Exception exception);
}
