using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Contracts;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyValidator _validator;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator validator)
        : base(options, logger, encoder)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
            return AuthenticateResult.NoResult();

        var result = await _validator.ValidateAsync(apiKey, Context.RequestAborted).ConfigureAwait(false);
        if (result is null)
            return AuthenticateResult.Fail("Invalid API key.");

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, result.Subject) }
            .Concat(result.Claims.Select(c => new Claim(c.Type, c.Value))),
            Scheme.Name);

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    private string? GetApiKey()
    {
        var headerName = Options.HeaderName;
        var value = Context.Request.Headers[headerName].FirstOrDefault();
        if (!string.IsNullOrEmpty(value))
            return value;
        if (!string.IsNullOrEmpty(Options.QueryParameterName))
            return Context.Request.Query[Options.QueryParameterName].FirstOrDefault();
        return null;
    }
}

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = "X-Api-Key";
    public string? QueryParameterName { get; set; }
}
