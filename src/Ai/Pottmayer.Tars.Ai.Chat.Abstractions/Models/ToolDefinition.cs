namespace Pottmayer.Tars.Ai.Chat.Abstractions.Models;

/// <summary>
/// A tool the model is allowed to call. <see cref="ParametersJsonSchema"/> is a JSON Schema object (as
/// a raw string) describing the arguments; the provider renders it into its own tool-definition shape.
/// </summary>
public sealed record ToolDefinition(
    string Name,
    string Description,
    string ParametersJsonSchema);
