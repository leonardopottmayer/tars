namespace Pottmayer.Tars.Security.Authorization.Abstractions;

/// <summary>
/// Error codes returned when a subject lacks a permission.
/// </summary>
public static class AuthorizationErrors
{
    /// <summary>The subject does not hold the permission, or the request has no subject.</summary>
    public const string Denied = "permission.denied";
}
