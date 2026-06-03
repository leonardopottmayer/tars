namespace Pottmayer.Tars.Security.Identity.Abstractions.Enums;

/// <summary>
/// Supported credential flow types for authentication.
/// </summary>
public enum CredentialFlowType
{
    Password = 0,
    MagicLink = 1,
    OAuth = 2,
    ApiKey = 3,
    RefreshToken = 4
}
