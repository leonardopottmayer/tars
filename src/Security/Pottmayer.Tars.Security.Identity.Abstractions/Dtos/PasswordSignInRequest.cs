namespace Pottmayer.Tars.Security.Identity.Abstractions.Dtos;

/// <summary>
/// Request for password-based sign-in.
/// </summary>
public sealed record PasswordSignInRequest
{
    /// <summary>
    /// Username
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Password
    /// </summary>
    public required string Password { get; init; }
}
