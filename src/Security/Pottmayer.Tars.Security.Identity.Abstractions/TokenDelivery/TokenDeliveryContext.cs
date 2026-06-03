using Pottmayer.Tars.Security.Identity.Abstractions.Enums;

namespace Pottmayer.Tars.Security.Identity.Abstractions.TokenDelivery;

/// <summary>
/// Context used by TokenDeliveryPolicy to decide effective delivery mode (e.g. for Hybrid).
/// Framework-agnostic; the HTTP layer fills this from request.
/// </summary>
public sealed class TokenDeliveryContext
{
    /// <summary>True if Authorization header (Bearer) is present.</summary>
    public bool HasAuthorizationHeader { get; init; }

    /// <summary>True if the configured auth cookie is present.</summary>
    public bool HasAuthCookie { get; init; }

    /// <summary>Value of the client-type header (e.g. "web", "api") if present.</summary>
    public string? ClientTypeHeaderValue { get; init; }

    /// <summary>Configured default mode and hybrid rules.</summary>
    public TokenDeliveryMode ConfiguredMode { get; init; }
}
