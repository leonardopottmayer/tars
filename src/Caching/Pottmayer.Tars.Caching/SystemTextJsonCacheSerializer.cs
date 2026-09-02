using Pottmayer.Tars.Caching.Abstractions;
using System.Text.Json;

namespace Pottmayer.Tars.Caching
{
    /// <summary>
    /// Default <see cref="ICacheSerializer"/> backed by <c>System.Text.Json</c>. Serializes to UTF-8 bytes
    /// using web defaults (camelCase) unless custom <see cref="JsonSerializerOptions"/> are supplied.
    /// </summary>
    public sealed class SystemTextJsonCacheSerializer : ICacheSerializer
    {
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Creates the serializer.
        /// </summary>
        /// <param name="options">
        /// JSON options to use. When <c>null</c>, <see cref="JsonSerializerDefaults.Web"/> defaults apply.
        /// </param>
        public SystemTextJsonCacheSerializer(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
            => JsonSerializer.SerializeToUtf8Bytes(value, _options);

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] data)
            => JsonSerializer.Deserialize<T>(data, _options);
    }
}
