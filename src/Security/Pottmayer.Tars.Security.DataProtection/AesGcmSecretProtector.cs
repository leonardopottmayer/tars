using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.DataProtection.Abstractions;
using Pottmayer.Tars.Security.DataProtection.Options;

namespace Pottmayer.Tars.Security.DataProtection;

/// <summary>
/// AES-256-GCM protector. Each value gets a fresh random nonce and an authentication tag, and is
/// stamped with the version of the key that encrypted it so keys can rotate without rewriting stored
/// ciphertext.
/// </summary>
/// <remarks>
/// Wire format: <c>{keyVersion}.{base64(nonce ‖ ciphertext ‖ tag)}</c>. The version is the stable
/// label from <see cref="DataProtectionOptions.Keys"/>; nonce is 12 bytes, tag is 16.
/// </remarks>
internal sealed class AesGcmSecretProtector : ISecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32; // 256-bit

    private readonly IReadOnlyDictionary<string, byte[]> _keys;
    private readonly string _activeVersion;

    public AesGcmSecretProtector(IOptions<DataProtectionOptions> options)
    {
        var value = options.Value;

        if (string.IsNullOrWhiteSpace(value.ActiveKeyVersion))
            throw new InvalidOperationException($"{DataProtectionOptions.SectionName}:ActiveKeyVersion is required.");

        if (value.Keys.Count == 0)
            throw new InvalidOperationException($"{DataProtectionOptions.SectionName}:Keys must contain at least one key.");

        var keys = new Dictionary<string, byte[]>(value.Keys.Count);
        foreach (var (version, material) in value.Keys)
        {
            byte[] key;
            try
            {
                key = Convert.FromBase64String(material);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException($"Data protection key '{version}' is not valid base64.");
            }

            if (key.Length != KeySize)
                throw new InvalidOperationException(
                    $"Data protection key '{version}' must be {KeySize} bytes ({KeySize * 8}-bit); got {key.Length}.");

            keys[version] = key;
        }

        if (!keys.ContainsKey(value.ActiveKeyVersion))
            throw new InvalidOperationException(
                $"{DataProtectionOptions.SectionName}:ActiveKeyVersion '{value.ActiveKeyVersion}' is not present in Keys.");

        _keys = keys;
        _activeVersion = value.ActiveKeyVersion;
    }

    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var key = _keys[_activeVersion];
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(key, TagSize))
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[NonceSize + cipherBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, 0, payload, NonceSize, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, payload, NonceSize + cipherBytes.Length, TagSize);

        return $"{_activeVersion}.{Convert.ToBase64String(payload)}";
    }

    public string Unprotect(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        var separator = ciphertext.IndexOf('.');
        if (separator <= 0)
            throw new SecretProtectionException("Protected value is malformed: missing key-version prefix.");

        var version = ciphertext[..separator];
        if (!_keys.TryGetValue(version, out var key))
            throw new SecretProtectionException(
                $"Protected value references key version '{version}', which is not configured.");

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(ciphertext[(separator + 1)..]);
        }
        catch (FormatException)
        {
            throw new SecretProtectionException("Protected value is malformed: payload is not valid base64.");
        }

        if (payload.Length < NonceSize + TagSize)
            throw new SecretProtectionException("Protected value is malformed: payload is too short.");

        var cipherLength = payload.Length - NonceSize - TagSize;
        var nonce = payload.AsSpan(0, NonceSize);
        var cipherBytes = payload.AsSpan(NonceSize, cipherLength);
        var tag = payload.AsSpan(NonceSize + cipherLength, TagSize);
        var plainBytes = new byte[cipherLength];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }
        catch (CryptographicException)
        {
            throw new SecretProtectionException("Protected value failed authentication: wrong key or tampered ciphertext.");
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}
