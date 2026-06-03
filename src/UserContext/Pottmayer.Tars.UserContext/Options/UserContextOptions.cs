namespace Pottmayer.Tars.UserContext.Options;

/// <summary>
/// Options for user context and claims-based user resolution.
/// </summary>
public class UserContextOptions
{
    /// <summary>
    /// Default configuration section name for UserContextOptions.
    /// </summary>
    public const string SectionName = "Tars:UserContext";

    /// <summary>
    /// Validation error message used when options validation fails.
    /// </summary>
    public const string ValidationErrorMessage = "Invalid UserContextOptions.";

    /// <summary>
    /// When true, a failed conversion of a claim value to a property type throws; when false, the property is left at default.
    /// Default: true.
    /// </summary>
    public bool ThrowOnConversionError { get; set; } = true;

    /// <summary>
    /// When true, missing or empty user id (sub/NameIdentifier/etc.) for an authenticated principal throws; when false, resolution may proceed with null userId.
    /// Default: true.
    /// </summary>
    public bool ThrowOnMissingRequiredUserId { get; set; } = true;

    /// <summary>
    /// When true, if no authenticated principal or required claims are present, the factory will use the registered <c>IFallbackUserProvider&lt;TUser&gt;</c> (if any) to provide a default user.
    /// Default: true.
    /// </summary>
    public bool UseFallbackUserWhenAnonymous { get; set; } = true;
}
