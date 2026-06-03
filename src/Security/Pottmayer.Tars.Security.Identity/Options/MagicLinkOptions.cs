namespace Pottmayer.Tars.Security.Identity.Options;

/// <summary>
/// Magic link options.
/// </summary>
public sealed class MagicLinkOptions
{
    /// <summary>TTL for the magic link token.</summary>
    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromMinutes(15);
}
