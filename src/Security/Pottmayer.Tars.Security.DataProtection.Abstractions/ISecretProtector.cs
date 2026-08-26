namespace Pottmayer.Tars.Security.DataProtection.Abstractions;

/// <summary>
/// Symmetric protection for secrets held at rest — refresh tokens, API keys, PKCE verifiers.
/// The key lives in configuration, never in the database, so a database dump alone yields nothing
/// usable.
/// </summary>
/// <remarks>
/// Ciphertext carries a key-version prefix, so the active key can be rotated without a
/// re-encrypt-everything event: old values still decrypt under their original key while new writes
/// use the current one.
/// </remarks>
public interface ISecretProtector
{
    /// <summary>Encrypts <paramref name="plaintext"/> under the active key. The result is opaque and safe to persist.</summary>
    string Protect(string plaintext);

    /// <summary>
    /// Decrypts a value produced by <see cref="Protect"/>. Throws <see cref="SecretProtectionException"/>
    /// when the value is malformed, its key version is unknown, or the authentication tag does not verify.
    /// </summary>
    string Unprotect(string ciphertext);
}
