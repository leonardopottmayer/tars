namespace Pottmayer.Tars.Security.Identity.Abstractions.Transport;

/// <summary>
/// Transport-agnostic representation of an inbound request for token reading.
/// The HTTP adapter fills this from HttpContext; other adapters fill it from their own sources.
/// </summary>
public sealed class TokenReadContext
{
    public IReadOnlyDictionary<string, string[]> Headers { get; init; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Cookies { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, object?> Items { get; init; } =
        new Dictionary<string, object?>();
}
