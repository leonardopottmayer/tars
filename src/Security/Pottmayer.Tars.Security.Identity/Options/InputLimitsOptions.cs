namespace Pottmayer.Tars.Security.Identity.Options;

/// <summary>
/// Configurable maximum lengths for identity endpoint inputs.
/// Protects against CPU-exhaustion DoS attacks (e.g. BCrypt with extremely large password inputs).
/// Set any limit to 0 or a negative value to disable that specific check.
/// </summary>
public sealed class InputLimitsOptions
{
    /// <summary>Maximum allowed length for username/email on password sign-in. Default: 256.</summary>
    public int MaxUsernameLength { get; init; } = 256;

    /// <summary>
    /// Maximum allowed length for passwords on sign-in. Default: 1024.
    /// BCrypt silently truncates passwords over 72 bytes; very large inputs are a DoS vector.
    /// </summary>
    public int MaxPasswordLength { get; init; } = 1024;

    /// <summary>Maximum allowed length for magic link target (e.g. email address). Default: 512.</summary>
    public int MaxMagicLinkTargetLength { get; init; } = 512;

    /// <summary>Maximum allowed length for magic link ReturnUrl. Default: 2048.</summary>
    public int MaxReturnUrlLength { get; init; } = 2048;

    /// <summary>Maximum allowed length for API key header values. Default: 512.</summary>
    public int MaxApiKeyLength { get; init; } = 512;
}
