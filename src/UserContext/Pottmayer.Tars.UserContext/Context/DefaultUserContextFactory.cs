using Microsoft.Extensions.Options;
using Pottmayer.Tars.UserContext.Abstractions;
using Pottmayer.Tars.UserContext.Abstractions.Context;
using Pottmayer.Tars.UserContext.Options;
using System.Security.Claims;

namespace Pottmayer.Tars.UserContext.Context;

/// <summary>
/// Creates user context from the current principal using a resolver.
/// When no authenticated principal or required claims are present, an optional <see cref="IFallbackUserProvider{TUser}"/> may provide a default user.
/// </summary>
/// <typeparam name="TUser">The user type.</typeparam>
public sealed class DefaultUserContextFactory<TUser> : IUserContextFactory<TUser>
    where TUser : class
{
    private static readonly string[] UserIdClaimTypes = new[]
    {
        ClaimTypes.NameIdentifier,
        "sub",
        "uid",
        "user_id"
    };

    private readonly ICurrentPrincipalAccessor _principalAccessor;
    private readonly IUserResolver<TUser> _resolver;
    private readonly IOptionsMonitor<UserContextOptions> _options;
    private readonly IFallbackUserProvider<TUser>? _fallbackUserProvider;

    /// <summary>
    /// Creates a new factory.
    /// </summary>
    /// <param name="principalAccessor">Provides the current request's claims principal.</param>
    /// <param name="resolver">Resolves the typed user from the principal.</param>
    /// <param name="options">The user context options, read at resolution time.</param>
    /// <param name="fallbackUserProvider">Optional provider of a default user when the principal is anonymous or missing required claims.</param>
    public DefaultUserContextFactory(
        ICurrentPrincipalAccessor principalAccessor,
        IUserResolver<TUser> resolver,
        IOptionsMonitor<UserContextOptions> options,
        IFallbackUserProvider<TUser>? fallbackUserProvider = null)
    {
        _principalAccessor = principalAccessor ?? throw new ArgumentNullException(nameof(principalAccessor));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _fallbackUserProvider = fallbackUserProvider;
    }

    /// <inheritdoc />
    public IUserContext<TUser> Create()
    {
        var principal = _principalAccessor.Principal;
        var options = _options.CurrentValue;

        if (principal is null || principal.Identity?.IsAuthenticated != true)
        {
            var fallback = TryGetFallbackUser(options);
            return new UserContext<TUser>(false, fallback);
        }

        var userId = GetUserId(principal);
        if (string.IsNullOrEmpty(userId))
        {
            if (options.ThrowOnMissingRequiredUserId)
                throw new InvalidOperationException(
                    "Authenticated principal has no user id. Expected one of the following claim types: " +
                    string.Join(", ", UserIdClaimTypes) + ".");

            var fallback = TryGetFallbackUser(options);
            return new UserContext<TUser>(false, fallback);
        }

        var user = _resolver.Resolve(principal);
        return new UserContext<TUser>(true, user);
    }

    /// <summary>Invokes the registered fallback provider, if any and if enabled by options.</summary>
    private TUser? TryGetFallbackUser(UserContextOptions options)
    {
        if (!options.UseFallbackUserWhenAnonymous)
            return null;

        return _fallbackUserProvider?.GetFallbackUserAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>Returns the first matching user id claim value, trying <see cref="UserIdClaimTypes"/> in order.</summary>
    private static string? GetUserId(ClaimsPrincipal principal)
    {
        foreach (var claimType in UserIdClaimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        return null;
    }
}
