using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.Token;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.DI;

public static class IdentityJwtBearerExtensions
{
    public const string DefaultJwtScheme = "Bearer";

    /// <summary>
    /// Wires up the Tars token reader into JwtBearer's OnMessageReceived event so tokens
    /// are extracted from headers or cookies according to the configured delivery mode.
    /// </summary>
    public static JwtBearerOptions ConfigureTarsIdentityJwtBearerEvents(this JwtBearerOptions options)
    {
        options.Events ??= new JwtBearerEvents();
        var existingOnMessageReceived = options.Events.OnMessageReceived;

        options.Events.OnMessageReceived = async ctx =>
        {
            if (existingOnMessageReceived is not null)
                await existingOnMessageReceived(ctx).ConfigureAwait(false);

            var reader = ctx.HttpContext.RequestServices.GetService<ITokenInputReader>();
            if (reader is not null)
            {
                var readContext = HttpContextTokenBridge.CreateReadContext(ctx.HttpContext);
                ctx.Token = reader.ReadAccessToken(readContext);
            }
        };

        return options;
    }
}
