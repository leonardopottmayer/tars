namespace Pottmayer.Tars.Caching.Abstractions
{
    public interface ICacheSerializer
    {
        byte[] Serialize<T>(T value);
        T? Deserialize<T>(byte[] data);
    }
}
