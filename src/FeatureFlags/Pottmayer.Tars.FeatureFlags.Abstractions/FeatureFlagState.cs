namespace Pottmayer.Tars.FeatureFlags.Abstractions;

/// <summary>
/// The current state of a declared flag.
/// </summary>
/// <param name="Definition">The flag as declared in code.</param>
/// <param name="Override">The stored override, or <c>null</c> when the default applies.</param>
/// <param name="Enabled">The effective state: the override or the default, and every ancestor enabled.</param>
public sealed record FeatureFlagState(FeatureFlagDefinition Definition, bool? Override, bool Enabled);
