using System.Text;

namespace Pottmayer.Tars.Ai.Chat.Abstractions.Models;

/// <summary>
/// One chat completion request. <see cref="Model"/> is chosen per call (the caller picks it from the
/// user's profile), so the same client instance serves whatever model the request names. Pass
/// <see cref="Temperature"/> <c>0</c> for the deterministic output a command pipeline wants.
/// </summary>
public sealed record ChatRequest(
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    IReadOnlyList<ToolDefinition>? Tools = null,
    double? Temperature = null,
    string? ApiKey = null)
{
    /// <summary>
    /// The provider credential for this one call. When set, it overrides whatever key the provider was
    /// configured with — the case where each end user brings their own key. Leave <c>null</c> to use the
    /// configured default. Ignored by providers that need no credential (e.g. a self-hosted endpoint).
    /// </summary>
    public string? ApiKey { get; init; } = ApiKey;

    // Keep the secret out of ToString/logs: records print every member by default.
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Model = ").Append(Model);
        builder.Append(", Messages = ").Append(Messages);
        builder.Append(", Tools = ").Append(Tools?.ToString() ?? "null");
        builder.Append(", Temperature = ").Append(Temperature);
        builder.Append(", ApiKey = ").Append(ApiKey is null ? "null" : "***");
        return true;
    }
}
