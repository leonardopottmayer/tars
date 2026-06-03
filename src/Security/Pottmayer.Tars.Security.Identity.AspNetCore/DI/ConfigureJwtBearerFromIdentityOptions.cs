using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pottmayer.Tars.Security.Identity.Abstractions.Contracts;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;
using Pottmayer.Tars.Security.Identity.Options;
using System.Text;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.DI;

internal sealed class ConfigureJwtBearerFromIdentityOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IOptionsMonitor<IdentityOptions> _identityOptionsMonitor;
    private readonly IOptionsMonitor<IdentityAspNetCoreOptions> _identityAspNetCoreOptionsMonitor;

    public ConfigureJwtBearerFromIdentityOptions(
        IOptionsMonitor<IdentityOptions> identityOptionsMonitor,
        IOptionsMonitor<IdentityAspNetCoreOptions> identityAspNetCoreOptionsMonitor)
    {
        _identityOptionsMonitor = identityOptionsMonitor ?? throw new ArgumentNullException(nameof(identityOptionsMonitor));
        _identityAspNetCoreOptionsMonitor = identityAspNetCoreOptionsMonitor ?? throw new ArgumentNullException(nameof(identityAspNetCoreOptionsMonitor));
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != IdentityJwtBearerExtensions.DefaultJwtScheme)
            return;

        var jwt = _identityOptionsMonitor.CurrentValue.Jwt;
        if (jwt is null || string.IsNullOrWhiteSpace(jwt.SigningKey))
            return;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = jwt.ClockSkew,
            RequireExpirationTime = true
        };
        options.RequireHttpsMetadata = _identityAspNetCoreOptionsMonitor.CurrentValue.Jwt.RequireHttpsMetadata;

        options.Events ??= new JwtBearerEvents();
        var existingOnTokenValidated = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async ctx =>
        {
            if (existingOnTokenValidated is not null)
                await existingOnTokenValidated(ctx).ConfigureAwait(false);

            if (ctx.Result?.Failure is not null)
                return;

            var revocation = ctx.HttpContext.RequestServices.GetService(typeof(ITokenRevocationService)) as ITokenRevocationService;
            if (revocation is null)
                return;

            var jti = ctx.Principal?.FindFirst("jti")?.Value;
            if (!string.IsNullOrEmpty(jti))
            {
                var isRevoked = await revocation.IsRevokedAsync(jti, ctx.HttpContext.RequestAborted).ConfigureAwait(false);
                if (isRevoked)
                {
                    ctx.Fail("Token has been revoked.");
                    return;
                }
            }

            var sv = ctx.Principal?.FindFirst("sv")?.Value;
            var sub = ctx.Principal?.FindFirst("sub")?.Value ?? ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(sv) && long.TryParse(sv, out var sessionVersion) && !string.IsNullOrEmpty(sub))
            {
                var currentSv = await revocation.GetSessionVersionAsync(sub, ctx.HttpContext.RequestAborted).ConfigureAwait(false);
                if (sessionVersion < currentSv)
                    ctx.Fail("Token has been revoked.");
            }
        };
    }

    public void Configure(JwtBearerOptions options) => Configure(IdentityJwtBearerExtensions.DefaultJwtScheme, options);
}
