namespace Pottmayer.Tars.FeatureFlags.Abstractions;

/// <summary>
/// A feature flag as declared in code by the module that owns it.
/// </summary>
/// <param name="Key">Unique key, for example <c>finances.import</c>.</param>
/// <param name="DefaultEnabled">The state used while no override is stored.</param>
/// <param name="Parent">Key of the parent flag. A flag is off whenever its parent is off.</param>
public sealed record FeatureFlagDefinition(string Key, bool DefaultEnabled, string? Parent = null);
