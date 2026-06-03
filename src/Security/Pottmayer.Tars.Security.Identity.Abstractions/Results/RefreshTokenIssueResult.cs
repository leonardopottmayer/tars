namespace Pottmayer.Tars.Security.Identity.Abstractions.Results;

/// <summary>
/// Result of issuing a refresh token.
/// </summary>
public sealed record RefreshTokenIssueResult(
    string OpaqueToken,
    string Id,
    DateTimeOffset ExpiresAt,
    string Subject,
    IReadOnlyList<ClaimData> Claims);
