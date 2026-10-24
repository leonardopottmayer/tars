using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pottmayer.Tars.Security.Identity.Abstractions.Contracts;
using Pottmayer.Tars.Security.Identity.Abstractions.Token;
using Pottmayer.Tars.Security.Identity.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Pottmayer.Tars.Security.Identity.Jwt;

/// <summary>
/// Validates JWT access tokens (signature, claims, expiration). Optionally checks revocation.
/// </summary>
public sealed class JwtTokenValidator : ITokenValidator
{
    private readonly IOptionsMonitor<SecurityIdentityOptions> _optionsMonitor;
    private readonly ITokenRevocationService? _revocationService;

    // The validator is scoped: warn about missing revocation once per process, not once per request.
    private static int _missingRevocationWarned;

    public JwtTokenValidator(
        IOptionsMonitor<SecurityIdentityOptions> optionsMonitor,
        ITokenRevocationService? revocationService = null,
        ILogger<JwtTokenValidator>? logger = null)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _revocationService = revocationService;

        if (revocationService is null && logger is not null && Interlocked.Exchange(ref _missingRevocationWarned, 1) == 0)
            logger.LogWarning("No ITokenRevocationService is registered: revoked access tokens and signed-out sessions stay valid until they expire. Register one with AddTarsIdentityTokenRevocationService.");
    }

    public async ValueTask<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        cancellationToken.ThrowIfCancellationRequested();

        var jwt = _optionsMonitor.CurrentValue.Jwt;
        if (jwt is null || string.IsNullOrWhiteSpace(jwt.SigningKey))
            return null;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = jwt.ClockSkew,
            RequireExpirationTime = true,
            // Only the algorithm the issuer signs with; anything else is rejected.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
                return null;

            var jti = jwtToken.Id;
            if (!string.IsNullOrEmpty(jti) && _revocationService is not null)
            {
                var isRevoked = await _revocationService.IsRevokedAsync(jti, cancellationToken).ConfigureAwait(false);
                if (isRevoked)
                    return null;
            }

            var svClaim = principal.FindFirst("sv")?.Value;
            if (!string.IsNullOrEmpty(svClaim) && long.TryParse(svClaim, out var sv) && _revocationService is not null)
            {
                var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
                if (!string.IsNullOrEmpty(sub))
                {
                    var currentSv = await _revocationService.GetSessionVersionAsync(sub, cancellationToken).ConfigureAwait(false);
                    if (sv < currentSv)
                        return null;
                }
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask<bool> IsValidAsync(string token, CancellationToken cancellationToken = default)
        => await ValidateAsync(token, cancellationToken).ConfigureAwait(false) is not null;
}
