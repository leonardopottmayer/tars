using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Services;

/// <summary>
/// Issues, consumes, and revokes refresh tokens with rotation and reuse detection.
/// </summary>
public interface IRefreshTokenService
{
    ValueTask<RefreshTokenIssueResult> IssueAsync(
        string subject,
        IReadOnlyList<ClaimData> claims,
        IReadOnlyDictionary<string, object?>? metadata,
        CancellationToken cancellationToken = default);

    ValueTask<RefreshTokenConsumeResult?> ConsumeAsync(string opaqueToken, CancellationToken cancellationToken = default);

    ValueTask RevokeAsync(string opaqueToken, CancellationToken cancellationToken = default);
}
