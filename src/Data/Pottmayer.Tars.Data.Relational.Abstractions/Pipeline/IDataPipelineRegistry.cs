namespace Pottmayer.Tars.Data.Relational.Abstractions.Pipeline;

public interface IDataPipelineRegistry
{
    void Register(DataPipelineDefinition definition);
    DataPipelineDefinition Get(string databaseKey);
    bool TryGet(string databaseKey, out DataPipelineDefinition? definition);
    IReadOnlyList<DataPipelineDefinition> GetAll();
}
