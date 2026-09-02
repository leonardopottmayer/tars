using Pottmayer.Tars.Security.Identity.Abstractions.Dtos;
using Pottmayer.Tars.Security.Identity.Abstractions.Enums;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Transport;

/// <summary>
/// Writes tokens to a transport-agnostic <see cref="TokenWriteContext"/>.
/// Implementations live in adapter projects (e.g. Identity.AspNetCore).
/// </summary>
public interface ITokenOutputWriter
{
    /// <summary>
    /// Writes the token response to the given context using the effective delivery mode.
    /// </summary>
    /// <param name="context">The outbound response context to write to.</param>
    /// <param name="tokenResponse">The tokens and metadata to deliver.</param>
    /// <param name="effectiveMode">The delivery mode to use (cookie, header, hybrid, or body).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAsync(
        TokenWriteContext context,
        TokenResponse tokenResponse,
        TokenDeliveryMode effectiveMode,
        CancellationToken cancellationToken = default);
}
