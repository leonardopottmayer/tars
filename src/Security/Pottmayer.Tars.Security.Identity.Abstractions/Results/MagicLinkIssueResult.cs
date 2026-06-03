namespace Pottmayer.Tars.Security.Identity.Abstractions.Results;

/// <summary>
/// Result of issuing a magic link token.
/// </summary>
public sealed record MagicLinkIssueResult(string Token, DateTimeOffset ExpiresAt);
