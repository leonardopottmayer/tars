using Pottmayer.Tars.Security.Identity.Abstractions.Stores;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Results;

/// <summary>
/// Result of consuming a refresh token.
/// </summary>
/// <param name="Payload">The token payload (subject, claims, metadata).</param>
/// <param name="ShouldIssueNewRefreshToken">
/// True when rotation is enabled — the caller must issue a new refresh token.
/// False when rotation is disabled — the client continues using the existing token.
/// </param>
public sealed record RefreshTokenConsumeResult(RefreshTokenPayload Payload, bool ShouldIssueNewRefreshToken);
