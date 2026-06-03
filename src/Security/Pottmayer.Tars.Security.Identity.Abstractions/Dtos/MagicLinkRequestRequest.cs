namespace Pottmayer.Tars.Security.Identity.Abstractions.Dtos;

/// <summary>
/// Request to request a magic link (send link to user).
/// </summary>
public sealed record MagicLinkRequestRequest
{
    /// <summary>Target identifier (e.g. email, phone). Application-defined.</summary>
    public required string Target { get; init; }

    /// <summary>Optional return URL after consume.</summary>
    public string? ReturnUrl { get; init; }
}
