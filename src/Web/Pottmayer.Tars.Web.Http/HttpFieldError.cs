using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http;

/// <summary>
/// Default implementation of a field validation error.
/// </summary>
/// <param name="Field">The field name.</param>
/// <param name="Message">The validation message.</param>
public sealed record HttpFieldError(string Field, string Message) : IHttpFieldError;
