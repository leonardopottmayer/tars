namespace Pottmayer.Tars.Web.Http.Abstractions;

public interface IPaginationInfo
{
    int Page { get; }
    int PageSize { get; }
    long TotalCount { get; }
    int TotalPages { get; }
}
