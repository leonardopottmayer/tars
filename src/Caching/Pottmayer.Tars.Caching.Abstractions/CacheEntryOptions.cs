namespace Pottmayer.Tars.Caching.Abstractions
{
    public sealed record CacheEntryOptions(
        TimeSpan? AbsoluteExpirationRelativeToNow = null,
        TimeSpan? SlidingExpiration = null);
}
