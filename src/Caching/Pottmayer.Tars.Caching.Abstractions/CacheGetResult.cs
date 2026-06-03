namespace Pottmayer.Tars.Caching.Abstractions
{
    public readonly record struct CacheGetResult<T>(bool Found, T? Value);
}
