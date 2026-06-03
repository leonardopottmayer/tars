using Pottmayer.Tars.Security.Identity.Abstractions.Dtos;
using Pottmayer.Tars.Security.Identity.Abstractions.Enums;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Transport;

/// <summary>
/// Writes tokens to a transport-agnostic <see cref="TokenWriteContext"/>.
/// Implementations live in adapter projects (e.g. Identity.AspNetCore).
/// </summary>
public interface ITokenOutputWriter
{
    Task WriteAsync(
        TokenWriteContext context,
        TokenResponse tokenResponse,
        TokenDeliveryMode effectiveMode,
        CancellationToken cancellationToken = default);
}
