using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pottmayer.Tars.Security.Identity.Abstractions.Contracts;
using Pottmayer.Tars.Security.Identity.Abstractions.Results;
using Pottmayer.Tars.Security.Identity.Abstractions.Services;
using Pottmayer.Tars.Security.Identity.AspNetCore.Endpoints;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class ReturnUrlValidationTests
{
    private const string AllowedOrigin = "https://app.example.com";

    [Theory]
    [InlineData("https://evil.example.com/steal")]
    [InlineData("//evil.example.com")]
    [InlineData("/\\evil.example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://app.example.com.evil.com/")]
    public async Task OAuth_challenge_rejects_return_urls_outside_the_app(string returnUrl)
    {
        var (status, challenged) = await OAuthChallenge(returnUrl);

        status.Should().Be(StatusCodes.Status400BadRequest);
        challenged.Should().BeFalse();
    }

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/")]
    [InlineData("https://app.example.com/after-login")]
    public async Task OAuth_challenge_accepts_local_paths_and_allowed_origins(string returnUrl)
    {
        var (_, challenged) = await OAuthChallenge(returnUrl);

        challenged.Should().BeTrue();
    }

    [Fact]
    public async Task Magic_link_request_rejects_a_return_url_outside_the_app()
    {
        var sender = new Mock<IMagicLinkSender>();

        var status = await RequestMagicLink("https://evil.example.com", sender);

        status.Should().Be(StatusCodes.Status400BadRequest);
        sender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Magic_link_request_accepts_a_local_return_url()
    {
        var sender = new Mock<IMagicLinkSender>();

        var status = await RequestMagicLink("/dashboard", sender);

        status.Should().Be(StatusCodes.Status200OK);
    }

    private static async Task<(int Status, bool Challenged)> OAuthChallenge(string returnUrl)
    {
        var auth = new CapturingAuthenticationService();
        var http = await Invoke("TarsIdentity_OAuthChallenge", app => app.MapTarsIdentityOAuthChallengeEndpoint(),
            services => services.AddSingleton<IAuthenticationService>(auth),
            http =>
            {
                http.Request.RouteValues["provider"] = "google";
                http.Request.QueryString = QueryString.Create("returnUrl", returnUrl);
            });
        return (http.Response.StatusCode, auth.Challenged);
    }

    private static async Task<int> RequestMagicLink(string returnUrl, Mock<IMagicLinkSender> sender)
    {
        var tokens = new Mock<IMagicLinkTokenService>();
        tokens.Setup(t => t.IssueAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MagicLinkIssueResult("token", DateTimeOffset.UtcNow.AddMinutes(15)));

        var http = await Invoke("TarsIdentity_RequestMagicLink", app => app.MapTarsIdentityRequestMagicLinkEndpoint(),
            services =>
            {
                services.AddSingleton(sender.Object);
                services.AddSingleton(tokens.Object);
                services.AddOptions<SecurityIdentityOptions>();
            },
            http =>
            {
                var json = $$"""{"target":"user@example.com","returnUrl":"{{returnUrl}}"}""";
                http.Request.Method = HttpMethods.Post;
                http.Request.ContentType = "application/json";
                var bytes = Encoding.UTF8.GetBytes(json);
                http.Request.Body = new MemoryStream(bytes);
                http.Request.ContentLength = bytes.Length;
                http.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestHasBody());
            });
        return http.Response.StatusCode;
    }

    private static async Task<HttpContext> Invoke(
        string endpointName,
        Action<WebApplication> map,
        Action<IServiceCollection> services,
        Action<HttpContext> request)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{IdentityAspNetCoreOptions.SectionName}:Endpoints:AllowedReturnUrlOrigins:0"] = AllowedOrigin,
            [$"{IdentityAspNetCoreOptions.SectionName}:MagicLink:BaseUrl"] = "https://app.example.com/magic-link",
        });
        builder.Services.AddOptions<IdentityAspNetCoreOptions>()
            .Bind(builder.Configuration.GetSection(IdentityAspNetCoreOptions.SectionName));
        services(builder.Services);

        await using var app = builder.Build();
        map(app);

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(d => d.Endpoints)
            .Single(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == endpointName);

        using var scope = app.Services.CreateScope();
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        http.Response.Body = new MemoryStream();
        request(http);
        await endpoint.RequestDelegate!(http);
        return http;
    }

    // DefaultHttpContext reports no body unless this feature says otherwise, so minimal APIs skip binding.
    private sealed class RequestHasBody : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }

    private sealed class CapturingAuthenticationService : IAuthenticationService
    {
        public bool Challenged { get; private set; }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            Challenged = true;
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }
}
