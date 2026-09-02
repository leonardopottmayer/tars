namespace Pottmayer.Tars.Data.Relational.Abstractions.Pipeline;

/// <summary>Registry of <see cref="DataPipelineDefinition"/>s keyed by database key.</summary>
public interface IDataPipelineRegistry
{
    /// <summary>Registers a pipeline definition.</summary>
    /// <param name="definition">The definition to register.</param>
    void Register(DataPipelineDefinition definition);

    /// <summary>Gets the pipeline definition for the given database key.</summary>
    /// <param name="databaseKey">Key identifying the logical database.</param>
    /// <returns>The registered definition.</returns>
    DataPipelineDefinition Get(string databaseKey);

    /// <summary>Attempts to get the pipeline definition for the given database key.</summary>
    /// <param name="databaseKey">Key identifying the logical database.</param>
    /// <param name="definition">When found, receives the definition; otherwise null.</param>
    /// <returns><c>true</c> when a definition exists for the key; otherwise <c>false</c>.</returns>
    bool TryGet(string databaseKey, out DataPipelineDefinition? definition);

    /// <summary>Returns all registered pipeline definitions.</summary>
    /// <returns>All registered definitions.</returns>
    IReadOnlyList<DataPipelineDefinition> GetAll();
}
