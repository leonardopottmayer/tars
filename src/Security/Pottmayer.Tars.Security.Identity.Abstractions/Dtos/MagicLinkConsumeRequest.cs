namespace Pottmayer.Tars.Security.Identity.Abstractions.Dtos;

/// <summary>
/// Request to consume a magic link (validate token and sign in).
/// </summary>
public sealed record MagicLinkConsumeRequest
{
    /// <summary>Token from the magic link (query or body).</summary>
    public required string Token { get; init; }
}
