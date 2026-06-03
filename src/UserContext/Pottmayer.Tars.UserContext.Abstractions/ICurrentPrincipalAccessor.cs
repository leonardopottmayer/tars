using System.Security.Claims;

namespace Pottmayer.Tars.UserContext.Abstractions;

/// <summary>
/// Provides the current request's claims principal (host-specific: HTTP context, etc.).
/// </summary>
public interface ICurrentPrincipalAccessor
{
    /// <summary>
    /// The current claims principal; null when no principal is available (e.g. background task).
    /// </summary>
    ClaimsPrincipal? Principal { get; }
}
