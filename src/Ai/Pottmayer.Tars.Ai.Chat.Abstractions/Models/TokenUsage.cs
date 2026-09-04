namespace Pottmayer.Tars.Ai.Chat.Abstractions.Models;

/// <summary>Token accounting for one completion, for cost and quality tracking. Zero when the provider did not report it.</summary>
public sealed record TokenUsage(int PromptTokens, int CompletionTokens);
