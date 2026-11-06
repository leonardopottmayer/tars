namespace Pottmayer.Tars.FeatureFlags.Abstractions;

/// <summary>
/// Error codes returned when a request reaches a disabled feature.
/// </summary>
public static class FeatureFlagErrors
{
    /// <summary>The requested feature is switched off in the system.</summary>
    public const string Disabled = "feature.disabled";
}
