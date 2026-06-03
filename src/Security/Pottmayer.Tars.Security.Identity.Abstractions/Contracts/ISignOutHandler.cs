namespace Pottmayer.Tars.Security.Identity.Abstractions.Contracts;

/// <summary>
/// Optional handler implemented by the host to perform side effects on sign-out (e.g. audit, cleanup).
/// The framework revokes tokens and then calls this if registered.
/// </summary>
public interface ISignOutHandler
{
    /// <summary>
    /// Called when the user signs out.
    /// </summary>
    /// <param name="subject">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask OnSignOutAsync(string subject, CancellationToken cancellationToken = default);
}
