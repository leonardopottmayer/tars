using System.Text.Json;

namespace Pottmayer.Tars.Ai.Chat.Abstractions.Models;

/// <summary>
/// A structured call the model chose to make: a tool <see cref="Name"/> and its
/// <see cref="Arguments"/> as raw JSON. This is the model's <em>only</em> meaningful output — the caller
/// validates the arguments against the tool's schema and decides whether to execute. The model never
/// invokes anything itself.
/// </summary>
public sealed record ToolCall(string Name, JsonElement Arguments, string? Id = null);
