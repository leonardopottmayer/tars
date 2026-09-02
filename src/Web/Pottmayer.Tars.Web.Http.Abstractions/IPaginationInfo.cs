namespace Pottmayer.Tars.Web.Http.Abstractions;

/// <summary>
/// Supplies pagination values for HTTP response headers.
/// </summary>
public interface IPaginationInfo
{
    /// <summary>Gets the current page number.</summary>
    int Page { get; }
    /// <summary>Gets the requested page size.</summary>
    int PageSize { get; }
    /// <summary>Gets the total number of items.</summary>
    long TotalCount { get; }
    /// <summary>Gets the total number of pages.</summary>
    int TotalPages { get; }
}
