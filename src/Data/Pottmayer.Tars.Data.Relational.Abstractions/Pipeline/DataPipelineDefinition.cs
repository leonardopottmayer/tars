namespace Pottmayer.Tars.Data.Relational.Abstractions.Pipeline;

public sealed class DataPipelineDefinition
{
    public required string DatabaseKey { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}
