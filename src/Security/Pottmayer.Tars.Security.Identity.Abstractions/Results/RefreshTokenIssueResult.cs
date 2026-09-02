namespace Pottmayer.Tars.Security.Identity.Abstractions.Results;

/// <summary>
/// Result of issuing a refresh token.
/// </summary>
/// <param name="OpaqueToken">The opaque token value handed to the client.</param>
/// <param name="Id">The token's stable identifier, used for lookup/revocation.</param>
/// <param name="ExpiresAt">When the token expires.</param>
/// <param name="Subject">The user identifier (subject) the token was issued for.</param>
/// <param name="Claims">The claims associated with the token.</param>
public sealed record RefreshTokenIssueResult(
    string OpaqueToken,
    string Id,
    DateTimeOffset ExpiresAt,
    string Subject,
    IReadOnlyList<ClaimData> Claims);
