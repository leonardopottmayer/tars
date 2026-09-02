namespace Pottmayer.Tars.Data.Relational.Abstractions.Pipeline;

/// <summary>Defines a data pipeline for a logical database, plus any associated metadata.</summary>
public sealed class DataPipelineDefinition
{
    /// <summary>Key identifying the logical database this pipeline serves.</summary>
    public required string DatabaseKey { get; init; }

    /// <summary>Additional pipeline metadata.</summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}
