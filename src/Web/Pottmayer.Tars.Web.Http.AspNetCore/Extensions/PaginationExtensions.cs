using Microsoft.AspNetCore.Http;
using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Extensions;

/// <summary>
/// Provides pagination HTTP response extensions.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>Writes the standard Tars pagination headers.</summary>
    /// <param name="response">The HTTP response to update.</param>
    /// <param name="pagination">The pagination values to write.</param>
    public static void WritePaginationHeaders(this HttpResponse response, IPaginationInfo pagination)
    {
        response.Headers["X-Pagination-Page"]       = pagination.Page.ToString();
        response.Headers["X-Pagination-PageSize"]   = pagination.PageSize.ToString();
        response.Headers["X-Pagination-TotalCount"] = pagination.TotalCount.ToString();
        response.Headers["X-Pagination-TotalPages"] = pagination.TotalPages.ToString();
    }
}
