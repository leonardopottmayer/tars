using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Web.Http.Internal;

internal static class TarsHttpMessages
{
    public const string NotFound            = "tars.http.not_found";
    public const string Validation          = "tars.http.validation";
    public const string BadRequest          = "tars.http.bad_request";
    public const string Conflict            = "tars.http.conflict";
    public const string Unauthorized        = "tars.http.unauthorized";
    public const string Forbidden           = "tars.http.forbidden";
    public const string InternalServerError = "tars.http.internal_server_error";

    public static string ForType(ErrorType type) => type switch
    {
        ErrorType.NotFound     => NotFound,
        ErrorType.Validation   => Validation,
        ErrorType.Business     => BadRequest,
        ErrorType.Conflict     => Conflict,
        ErrorType.Unauthorized => Unauthorized,
        ErrorType.Forbidden    => Forbidden,
        _                      => InternalServerError
    };

    public static IDictionary<string, IDictionary<string, string>> GetDefaultMessages()
        => new Dictionary<string, IDictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                [NotFound]            = "Resource not found.",
                [Validation]          = "One or more validation errors occurred.",
                [BadRequest]          = "Invalid request.",
                [Conflict]            = "A conflict occurred.",
                [Unauthorized]        = "Authentication required.",
                [Forbidden]           = "Access denied.",
                [InternalServerError] = "An unexpected error occurred."
            },
            ["pt-BR"] = new Dictionary<string, string>
            {
                [NotFound]            = "Recurso não encontrado.",
                [Validation]          = "Um ou mais erros de validação ocorreram.",
                [BadRequest]          = "Requisição inválida.",
                [Conflict]            = "Um conflito ocorreu.",
                [Unauthorized]        = "Autenticação necessária.",
                [Forbidden]           = "Acesso negado.",
                [InternalServerError] = "Ocorreu um erro inesperado."
            }
        };
}
