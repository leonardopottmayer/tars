using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Abstractions.Services;
using Pottmayer.Tars.Security.Identity.Abstractions.Stores;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Identity.Refresh;

/// <summary>
/// Issues and validates refresh tokens with rotation and reuse detection.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenStore _store;
    private readonly IOptionsMonitor<IdentityOptions> _optionsMonitor;

    private const int TokenByteLength = 32;
    private const int IdByteLength = 16;

    public RefreshTokenService(IRefreshTokenStore store, IOptionsMonitor<IdentityOptions> optionsMonitor)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }

    public async ValueTask<RefreshTokenIssueResult> IssueAsync(
        string subject,
        IReadOnlyList<ClaimData> claims,
        IReadOnlyDictionary<string, object?>? metadata,
        CancellationToken cancellationToken = default)
    {
        var refreshOpts = _optionsMonitor.CurrentValue.RefreshToken;
        var id = GenerateId();
        var secret = GenerateToken();
        var opaque = $"{id}:{secret}";
        var expiresAt = DateTimeOffset.UtcNow.Add(refreshOpts.Lifetime);

        await _store.StoreAsync(id, HashSecret(secret), subject, claims, expiresAt, metadata, cancellationToken).ConfigureAwait(false);

        return new RefreshTokenIssueResult(
            OpaqueToken: opaque,
            Id: id,
            ExpiresAt: expiresAt,
            Subject: subject,
            Claims: claims);
    }

    public async ValueTask<RefreshTokenConsumeResult?> ConsumeAsync(string opaqueToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueToken))
            return null;

        var colon = opaqueToken.IndexOf(':');
        if (colon <= 0 || colon >= opaqueToken.Length - 1)
            return null;

        var id = opaqueToken[..colon];
        var secretHash = HashSecret(opaqueToken[(colon + 1)..]);
        var refreshOpts = _optionsMonitor.CurrentValue.RefreshToken;

        if (refreshOpts.RotationEnabled)
        {
            var payload = await _store.GetAndRemoveAsync(id, secretHash, cancellationToken).ConfigureAwait(false);
            if (payload is null)
            {
                if (refreshOpts.ReuseDetectionEnabled)
                {
                    var subjectForReuse = await _store.TryGetSubjectForReuseAsync(id, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(subjectForReuse))
                        await _store.RevokeAllForSubjectAsync(subjectForReuse, cancellationToken).ConfigureAwait(false);
                }
                return null;
            }
            return new RefreshTokenConsumeResult(payload, ShouldIssueNewRefreshToken: true);
        }
        else
        {
            var payload = await _store.GetAsync(id, secretHash, cancellationToken).ConfigureAwait(false);
            if (payload is null)
                return null;
            return new RefreshTokenConsumeResult(payload, ShouldIssueNewRefreshToken: false);
        }
    }

    public ValueTask RevokeAsync(string opaqueToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueToken))
            return ValueTask.CompletedTask;

        var colon = opaqueToken.IndexOf(':');
        if (colon <= 0)
            return ValueTask.CompletedTask;

        var id = opaqueToken[..colon];
        return _store.RevokeAsync(id, cancellationToken);
    }

    private static string GenerateId()
    {
        var bytes = new byte[IdByteLength];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string GenerateToken()
    {
        var bytes = new byte[TokenByteLength];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashSecret(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
