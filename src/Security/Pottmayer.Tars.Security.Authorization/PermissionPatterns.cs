namespace Pottmayer.Tars.Security.Authorization;

/// <summary>
/// Matches permission keys against grant patterns: <c>*</c>, a prefix wildcard ending in <c>.*</c>,
/// or an exact key.
/// </summary>
public static class PermissionPatterns
{
    /// <summary>Whether <paramref name="key"/> is covered by <paramref name="pattern"/>.</summary>
    public static bool Matches(string pattern, string key)
    {
        if (pattern == "*")
            return true;

        if (pattern.EndsWith(".*", StringComparison.Ordinal))
            return key.AsSpan().StartsWith(pattern.AsSpan(0, pattern.Length - 1), StringComparison.Ordinal);

        return string.Equals(pattern, key, StringComparison.Ordinal);
    }
}
