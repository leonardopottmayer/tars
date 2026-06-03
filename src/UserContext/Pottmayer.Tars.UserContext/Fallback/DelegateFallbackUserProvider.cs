using Pottmayer.Tars.UserContext.Abstractions;

namespace Pottmayer.Tars.UserContext.Fallback;

/// <summary>
/// Fallback user provider that returns the user from a delegate.
/// </summary>
/// <typeparam name="TUser">The user type.</typeparam>
public sealed class DelegateFallbackUserProvider<TUser> : IFallbackUserProvider<TUser>
    where TUser : class
{
    private readonly Func<CancellationToken, Task<TUser?>> _getFallbackUserAsync;

    /// <summary>
    /// Creates a new provider that uses the given async delegate.
    /// </summary>
    public DelegateFallbackUserProvider(Func<CancellationToken, Task<TUser?>> getFallbackUserAsync)
    {
        _getFallbackUserAsync = getFallbackUserAsync ?? throw new ArgumentNullException(nameof(getFallbackUserAsync));
    }

    /// <summary>
    /// Creates a new provider that uses the given sync delegate (wrapped as async).
    /// </summary>
    public DelegateFallbackUserProvider(Func<TUser?> getFallbackUser)
    {
        if (getFallbackUser == null)
            throw new ArgumentNullException(nameof(getFallbackUser));
        _getFallbackUserAsync = _ => Task.FromResult(getFallbackUser());
    }

    /// <inheritdoc />
    public Task<TUser?> GetFallbackUserAsync(CancellationToken cancellationToken = default) =>
        _getFallbackUserAsync(cancellationToken);
}
