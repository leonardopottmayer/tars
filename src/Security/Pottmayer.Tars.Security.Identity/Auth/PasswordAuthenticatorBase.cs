using Pottmayer.Tars.Security.Identity.Abstractions.Contracts;
using Pottmayer.Tars.Security.Identity.Abstractions.Dtos;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Abstractions.Stores;

namespace Pottmayer.Tars.Security.Identity.Auth;

/// <summary>
/// Optional base class for password-based authentication.
/// Handles user lookup and claims building; the subclass implements only password verification.
/// </summary>
public abstract class PasswordAuthenticatorBase<TUser> : IPasswordAuthenticator
    where TUser : class
{
    private readonly IUserStore<TUser> _userStore;
    private readonly IClaimsProvider<TUser> _claimsProvider;

    protected PasswordAuthenticatorBase(IUserStore<TUser> userStore, IClaimsProvider<TUser> claimsProvider)
    {
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        _claimsProvider = claimsProvider ?? throw new ArgumentNullException(nameof(claimsProvider));
    }

    public async ValueTask<AuthenticationResult?> AuthenticateAsync(PasswordSignInRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await ResolveUserAsync(request, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return null;

        var valid = await VerifyPasswordAsync(user, request.Password, cancellationToken).ConfigureAwait(false);
        if (!valid)
            return null;

        var claims = await _claimsProvider.GetClaimsAsync(user, cancellationToken).ConfigureAwait(false);
        var subject = GetSubject(user);

        return new AuthenticationResult
        {
            Subject = subject,
            Claims = claims
        };
    }

    /// <summary>
    /// Returns the subject (user id) for the authenticated user. Must be overridden.
    /// </summary>
    protected abstract string GetSubject(TUser user);

    /// <summary>
    /// Verifies the password for the given user. Must be overridden.
    /// </summary>
    protected abstract ValueTask<bool> VerifyPasswordAsync(TUser user, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the user from the sign-in request. By default looks up by username.
    /// Override to look up by email or use a different strategy.
    /// </summary>
    protected virtual Task<TUser?> ResolveUserAsync(PasswordSignInRequest request, CancellationToken cancellationToken)
        => _userStore.FindByUsernameAsync(request.Username, cancellationToken);
}
