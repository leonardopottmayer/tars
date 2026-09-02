namespace Pottmayer.Tars.Observability.Options;

/// <summary>
/// Validates <see cref="ObservabilityOptions"/>.
/// </summary>
public static class ObservabilityOptionsValidation
{
    /// <summary>
    /// Validates the given options.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool Validate(ObservabilityOptions options) =>
        !options.Enabled || !string.IsNullOrWhiteSpace(options.ServiceName);
}
