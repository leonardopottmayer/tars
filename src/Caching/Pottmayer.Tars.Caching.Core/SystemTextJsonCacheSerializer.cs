using Pottmayer.Tars.Caching.Abstractions;
using System.Text.Json;

namespace Pottmayer.Tars.Caching.Core
{
    public sealed class SystemTextJsonCacheSerializer : ICacheSerializer
    {
        private readonly JsonSerializerOptions _options;

        public SystemTextJsonCacheSerializer(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public byte[] Serialize<T>(T value)
            => JsonSerializer.SerializeToUtf8Bytes(value, _options);

        public T? Deserialize<T>(byte[] data)
            => JsonSerializer.Deserialize<T>(data, _options);
    }
}
