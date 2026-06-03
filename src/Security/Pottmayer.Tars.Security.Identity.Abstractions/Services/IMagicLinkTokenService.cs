using Pottmayer.Tars.Security.Identity.Abstractions.Results;

namespace Pottmayer.Tars.Security.Identity.Abstractions.Services;

/// <summary>
/// Generates and consumes magic link tokens.
/// </summary>
public interface IMagicLinkTokenService
{
    ValueTask<MagicLinkIssueResult> IssueAsync(
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyDictionary<string, object?>?> ConsumeAsync(string token, CancellationToken cancellationToken = default);
}
