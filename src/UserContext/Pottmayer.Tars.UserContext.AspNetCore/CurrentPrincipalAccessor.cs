using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Pottmayer.Tars.UserContext.Abstractions;

namespace Pottmayer.Tars.UserContext.AspNetCore;

/// <summary>
/// Provides the current HTTP request's claims principal via <see cref="IHttpContextAccessor"/>.
/// When <see cref="HttpContext"/> is null (e.g. background task), returns null (treated as anonymous).
/// </summary>
public sealed class CurrentPrincipalAccessor : ICurrentPrincipalAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;
}
