using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Abstractions.Token;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Identity.Jwt;

/// <summary>
/// Issues JWT access tokens from authentication results.
/// </summary>
public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly IOptionsMonitor<IdentityOptions> _optionsMonitor;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenIssuer(IOptionsMonitor<IdentityOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }

    public ValueTask<IssuedTokenResult> IssueAsync(AuthenticationResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

        var jwt = _optionsMonitor.CurrentValue.Jwt;
        if (jwt is null || string.IsNullOrWhiteSpace(jwt.SigningKey))
            throw new InvalidOperationException("JwtOptions.SigningKey is required.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var now = DateTime.UtcNow;
        var expires = now.Add(jwt.AccessTokenLifetime);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, result.Subject),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat, Epoch(now).ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Exp, Epoch(expires).ToString(), ClaimValueTypes.Integer64)
        };

        if (result.SessionVersion.HasValue)
            claims.Add(new Claim("sv", result.SessionVersion.Value.ToString(), ClaimValueTypes.Integer64));

        foreach (var c in result.Claims)
            claims.Add(new Claim(c.Type, c.Value));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = _handler.CreateToken(descriptor);
        var tokenString = _handler.WriteToken(token);

        return ValueTask.FromResult(new IssuedTokenResult(tokenString, jti, Epoch(expires)));
    }

    private static long Epoch(DateTime utc) => new DateTimeOffset(utc).ToUnixTimeSeconds();
}
