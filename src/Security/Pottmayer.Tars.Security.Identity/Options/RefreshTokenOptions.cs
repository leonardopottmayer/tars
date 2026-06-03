namespace Pottmayer.Tars.Security.Identity.Options;

/// <summary>
/// Refresh token options.
/// </summary>
public sealed class RefreshTokenOptions
{
    /// <summary>Whether refresh tokens are enabled.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Refresh token lifetime.</summary>
    public TimeSpan Lifetime { get; init; } = TimeSpan.FromDays(7);

    /// <summary>Whether to rotate refresh token on each use (new token, invalidate previous).</summary>
    public bool RotationEnabled { get; init; } = true;

    /// <summary>Whether to detect reuse (if old token is used again, revoke all for that subject).</summary>
    public bool ReuseDetectionEnabled { get; init; } = true;
}
