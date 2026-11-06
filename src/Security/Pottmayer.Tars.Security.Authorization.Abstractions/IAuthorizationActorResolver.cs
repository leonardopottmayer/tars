namespace Pottmayer.Tars.Security.Authorization.Abstractions;

/// <summary>
/// Tells the mediator behavior on whose behalf a request runs. The application registers it, since
/// only it knows where the subject lives (a <c>UserId</c> on the request, the current principal, …).
/// </summary>
public interface IAuthorizationActorResolver
{
    /// <summary>The subject id, or <c>null</c> when the request has none (it is then denied).</summary>
    string? Resolve(object request);
}
