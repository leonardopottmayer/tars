namespace Pottmayer.Tars.Observability.Options;

public static class ObservabilityOptionsValidation
{
    public const string ValidationErrorMessage =
        "Tars observability options are invalid: ServiceName must be provided when observability is enabled.";

    public static bool Validate(ObservabilityOptions options) =>
        !options.Enabled || !string.IsNullOrWhiteSpace(options.ServiceName);
}
