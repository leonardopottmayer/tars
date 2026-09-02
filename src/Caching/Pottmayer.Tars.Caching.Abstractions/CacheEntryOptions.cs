namespace Pottmayer.Tars.Caching.Abstractions
{
    /// <summary>
    /// Per-entry expiration policy passed to write operations. Both fields are optional; when both are
    /// null the entry follows the provider default (if any) or never expires.
    /// </summary>
    /// <param name="AbsoluteExpirationRelativeToNow">
    /// Lifetime measured from the moment of writing, after which the entry expires regardless of access.
    /// </param>
    /// <param name="SlidingExpiration">
    /// Idle window that is renewed on each access; the entry expires once it goes unused for this long.
    /// </param>
    public sealed record CacheEntryOptions(
        TimeSpan? AbsoluteExpirationRelativeToNow = null,
        TimeSpan? SlidingExpiration = null);
}
