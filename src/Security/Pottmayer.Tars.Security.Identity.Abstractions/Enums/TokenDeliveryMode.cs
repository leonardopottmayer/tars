namespace Pottmayer.Tars.Security.Identity.Abstractions.Enums;

/// <summary>
/// Defines how the access token is delivered to the client.
/// </summary>
public enum TokenDeliveryMode
{
    /// <summary>Token only in HTTP-only cookie.</summary>
    CookieOnly = 0,

    /// <summary>Token only in Authorization header (Bearer).</summary>
    HeaderOnly = 1,

    /// <summary>Token only in response body (e.g. JSON).</summary>
    BodyOnly = 2,

    /// <summary>Deterministic choice based on request context (header, cookie, X-Client-Type, or configuration).</summary>
    Hybrid = 3
}
