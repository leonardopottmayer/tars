namespace Pottmayer.Tars.Web.Http.Abstractions;

public interface IHttpFieldError
{
    string Field { get; }
    string Message { get; }
}
