using Pottmayer.Tars.UserContext.Abstractions.Context;

namespace Pottmayer.Tars.UserContext.Context;

/// <summary>
/// Scoped accessor that lazily creates and caches the user context per request.
/// </summary>
/// <typeparam name="TUser">The user type.</typeparam>
public sealed class UserContextAccessor<TUser> : IUserContextAccessor<TUser>
    where TUser : class
{
    private readonly IUserContextFactory<TUser> _factory;
    private IUserContext<TUser>? _cached;

    /// <summary>
    /// Creates a new accessor.
    /// </summary>
    /// <param name="factory">The factory used to create the context.</param>
    public UserContextAccessor(IUserContextFactory<TUser> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc />
    public IUserContext<TUser> Context => _cached ??= _factory.Create();
}
