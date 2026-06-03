namespace Pottmayer.Tars.Security.Identity.Abstractions.Results;

/// <summary>
/// Result of an authentication attempt. Contains subject, claims and optional custom data when successful.
/// </summary>
public sealed class AuthenticationResult
{
    /// <summary>User identifier (subject).</summary>
    public required string Subject { get; init; }

    /// <summary>Claims to include in the issued token.</summary>
    public required IReadOnlyList<ClaimData> Claims { get; init; }

    /// <summary>Optional custom data for the application (e.g. profile, tenant).</summary>
    public IReadOnlyDictionary<string, object?>? CustomData { get; init; }

    /// <summary>Optional session version for stateful revocation (sv claim).</summary>
    public long? SessionVersion { get; init; }
}

/// <summary>
/// A single claim name/value pair.
/// </summary>
public sealed record ClaimData(string Type, string Value);
