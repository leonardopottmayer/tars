using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Web.Http.Abstractions;

public interface IHttpErrorMapper
{
    int MapToStatusCode(ErrorType errorType);
    IHttpErrorResponse Map(Error error);
    IHttpErrorResponse Map(Exception exception);
}
