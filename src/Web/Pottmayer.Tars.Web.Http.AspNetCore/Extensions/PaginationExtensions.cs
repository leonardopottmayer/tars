using Microsoft.AspNetCore.Http;
using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Extensions;

public static class PaginationExtensions
{
    public static void WritePaginationHeaders(this HttpResponse response, IPaginationInfo pagination)
    {
        response.Headers["X-Pagination-Page"]       = pagination.Page.ToString();
        response.Headers["X-Pagination-PageSize"]   = pagination.PageSize.ToString();
        response.Headers["X-Pagination-TotalCount"] = pagination.TotalCount.ToString();
        response.Headers["X-Pagination-TotalPages"] = pagination.TotalPages.ToString();
    }
}
