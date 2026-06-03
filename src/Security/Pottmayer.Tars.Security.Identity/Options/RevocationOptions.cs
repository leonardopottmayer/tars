using Pottmayer.Tars.Security.Identity.Abstractions.Enums;

namespace Pottmayer.Tars.Security.Identity.Options;

/// <summary>
/// Sign-out and revocation options.
/// </summary>
public sealed class RevocationOptions
{
    /// <summary>Sign-out mode: Stateless (only refresh) or Stateful (access + refresh).</summary>
    public SignOutMode SignOutMode { get; init; } = SignOutMode.Stateful;

    /// <summary>Strategy for stateful revocation: JtiBlacklist or SessionVersion.</summary>
    public RevocationStrategy Strategy { get; init; } = RevocationStrategy.JtiBlacklist;

    /// <summary>Whether stateful revocation is enabled (requires ITokenRevocationStore).</summary>
    public bool StatefulRevocationEnabled { get; init; } = true;
}
