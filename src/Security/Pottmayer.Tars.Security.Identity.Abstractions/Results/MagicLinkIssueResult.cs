namespace Pottmayer.Tars.Security.Identity.Abstractions.Results;

/// <summary>
/// Result of issuing a magic link token.
/// </summary>
/// <param name="Token">The opaque token to embed in the magic link.</param>
/// <param name="ExpiresAt">When the token expires.</param>
public sealed record MagicLinkIssueResult(string Token, DateTimeOffset ExpiresAt);
