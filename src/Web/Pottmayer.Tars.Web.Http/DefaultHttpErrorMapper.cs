using Pottmayer.Tars.Core.Localization.Abstractions;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.Internal;

namespace Pottmayer.Tars.Web.Http;

public sealed class DefaultHttpErrorMapper : IHttpErrorMapper
{
    private readonly IMessageProvider _messages;

    public DefaultHttpErrorMapper(IMessageProvider messages)
        => _messages = messages;

    public int MapToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.NotFound     => 404,
        ErrorType.Validation   => 422,
        ErrorType.Business     => 400,
        ErrorType.Conflict     => 409,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden    => 403,
        _                      => 500
    };

    public IHttpErrorResponse Map(Error error)
    {
        var messageKey = TarsHttpMessages.ForType(error.Type);
        var errorMessage = string.IsNullOrWhiteSpace(error.Message)
            ? _messages.Get(messageKey)
            : error.Message;

        return new HttpErrorResponse
        {
            Success    = false,
            ErrorCode  = error.Code,
            ErrorMessage = errorMessage,
            FieldErrors  = BuildFieldErrors(error)
        };
    }

    public IHttpErrorResponse Map(Exception exception)
        => new HttpErrorResponse
        {
            Success      = false,
            ErrorCode    = "INTERNAL_SERVER_ERROR",
            ErrorMessage = _messages.Get(TarsHttpMessages.InternalServerError)
        };

    private static IReadOnlyList<IHttpFieldError>? BuildFieldErrors(Error error)
    {
        if (error.Type != ErrorType.Validation || error.Metadata is null)
            return null;

        var list = error.Metadata
            .Select(kv => (IHttpFieldError)new HttpFieldError(kv.Key, kv.Value?.ToString() ?? string.Empty))
            .ToList();

        return list.Count > 0 ? list : null;
    }
}
