using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly IFallbackUserProvider<TUser>? _fallbackUserProvider;

    /// <summary>
    /// Creates a new factory.
    /// </summary>
    public DefaultUserContextFactory(
        ICurrentPrincipalAccessor principalAccessor,
        IUserResolver<TUser> resolver,
        IOptionsMonitor<UserContextOptions> options,
        IServiceProvider serviceProvider,
        IFallbackUserProvider<TUser>? fallbackUserProvider = null)
    {
        _principalAccessor = principalAccessor ?? throw new ArgumentNullException(nameof(principalAccessor));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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

    private TUser? TryGetFallbackUser(UserContextOptions options)
    {
        if (!options.UseFallbackUserWhenAnonymous)
            return null;

        return _fallbackUserProvider?.GetFallbackUserAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

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
