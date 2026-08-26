namespace Pottmayer.Tars.Security.DataProtection.Options;

/// <summary>Keys for <see cref="AesGcmSecretProtector"/>, bound from configuration.</summary>
/// <remarks>
/// Keys are kept out of the database on purpose (I3): in Docker they come from an environment
/// variable, in the homelab from a mounted secret. More than one may be present at a time so a
/// rotation is a background re-encrypt rather than a reconnect-everything event.
/// </remarks>
public sealed class DataProtectionOptions
{
    public const string SectionName = "Tars:Security:DataProtection";

    /// <summary>
    /// Available keys, by version label. Each value is a base64-encoded 256-bit (32-byte) key.
    /// The label is what gets stamped into the ciphertext, so it must be stable for a key's lifetime.
    /// </summary>
    public Dictionary<string, string> Keys { get; set; } = new();

    /// <summary>The version new values are encrypted under. Must be a key present in <see cref="Keys"/>.</summary>
    public string ActiveKeyVersion { get; set; } = string.Empty;
}
