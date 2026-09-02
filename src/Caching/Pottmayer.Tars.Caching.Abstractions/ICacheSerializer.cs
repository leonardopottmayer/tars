namespace Pottmayer.Tars.Caching.Abstractions
{
    /// <summary>
    /// Serializes and deserializes cached values to and from their byte representation, so providers
    /// that store opaque payloads (e.g. Redis) can persist arbitrary types.
    /// </summary>
    public interface ICacheSerializer
    {
        /// <summary>
        /// Serializes <paramref name="value"/> to a byte array.
        /// </summary>
        /// <typeparam name="T">Type of the value to serialize.</typeparam>
        /// <param name="value">Value to serialize.</param>
        /// <returns>The serialized payload.</returns>
        byte[] Serialize<T>(T value);

        /// <summary>
        /// Deserializes <paramref name="data"/> back into a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target type of the deserialized value.</typeparam>
        /// <param name="data">Serialized payload previously produced by <see cref="Serialize{T}(T)"/>.</param>
        /// <returns>The deserialized value, or <c>default</c> when the payload represents none.</returns>
        T? Deserialize<T>(byte[] data);
    }
}
