using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Abstractions.Services;
using Pottmayer.Tars.Security.Identity.Abstractions.Stores;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Identity.MagicLink;

/// <summary>
/// Generates and consumes magic link tokens.
/// </summary>
public sealed class MagicLinkTokenService : IMagicLinkTokenService
{
    private readonly IMagicLinkTokenStore _store;
    private readonly IOptionsMonitor<IdentityOptions> _optionsMonitor;

    private const int TokenByteLength = 32;

    public MagicLinkTokenService(IMagicLinkTokenStore store, IOptionsMonitor<IdentityOptions> optionsMonitor)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }

    public async ValueTask<MagicLinkIssueResult> IssueAsync(
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue.MagicLink;
        var token = GenerateToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(options.TokenLifetime);
        await _store.StoreAsync(token, payload, expiresAt, cancellationToken).ConfigureAwait(false);

        return new MagicLinkIssueResult(token, expiresAt);
    }

    public ValueTask<IReadOnlyDictionary<string, object?>?> ConsumeAsync(string token, CancellationToken cancellationToken = default)
        => _store.ConsumeAsync(token, cancellationToken);

    private static string GenerateToken()
    {
        var bytes = new byte[TokenByteLength];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
