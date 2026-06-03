using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http;

public sealed record HttpFieldError(string Field, string Message) : IHttpFieldError;
