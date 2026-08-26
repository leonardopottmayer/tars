namespace Pottmayer.Tars.Security.DataProtection.Abstractions;

/// <summary>
/// Raised when a value cannot be unprotected: it is malformed, references a key version that is not
/// configured, or fails authentication. Never carries the plaintext or the key.
/// </summary>
public sealed class SecretProtectionException(string message) : Exception(message);
