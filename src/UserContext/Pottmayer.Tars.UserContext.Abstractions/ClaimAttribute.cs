namespace Pottmayer.Tars.UserContext.Abstractions;

/// <summary>
/// Specifies the claim name used to populate the attributed property when resolving the user from claims.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ClaimAttribute : Attribute
{
    /// <summary>
    /// The claim type/name to use (e.g. "sub", "email").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="name">The claim name.</param>
    public ClaimAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
