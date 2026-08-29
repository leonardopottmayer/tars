namespace Pottmayer.Tars.Observability.Abstractions;

/// <summary>
/// Shared naming conventions for telemetry sources across the Pottmayer.Tars framework.
/// </summary>
/// <remarks>
/// Building blocks create their <see cref="System.Diagnostics.ActivitySource"/> and
/// <see cref="System.Diagnostics.Metrics.Meter"/> using these names, so a single wildcard
/// subscription in the observability wiring (<see cref="Wildcard"/>) captures every one of them
/// without the wiring having to know each family by name. This project deliberately has no
/// dependencies so any family can reference it without pulling in OpenTelemetry.
/// </remarks>
public static class TarsTelemetry
{
    /// <summary>Root prefix shared by every tars ActivitySource and Meter name.</summary>
    public const string RootName = "Pottmayer.Tars";

    /// <summary>
    /// Wildcard that matches every tars source and meter, for use with
    /// OpenTelemetry's <c>AddSource</c> / <c>AddMeter</c>.
    /// </summary>
    public const string Wildcard = "Pottmayer.Tars.*";

    /// <summary>
    /// Builds a source/meter name for a family, e.g. <c>Name("Messaging")</c> yields
    /// <c>"Pottmayer.Tars.Messaging"</c>.
    /// </summary>
    public static string Name(string family) => $"{RootName}.{family}";
}
