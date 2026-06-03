using Pottmayer.Tars.UserContext.Abstractions;

namespace Pottmayer.Tars.UserContext;

/// <summary>
/// Stores the current <see cref="IUserContext"/> in an <see cref="AsyncLocal{T}"/>,
/// making it available across the entire async call chain without depending on HTTP scope.
/// Safe for use in ASP.NET Core, workers, Blazor Server, and unit tests.
/// </summary>
public sealed class AsyncLocalUserContextAccessor : IUserContextAccessor
{
    private static readonly AsyncLocal<IUserContext?> _current = new();

    public IUserContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
