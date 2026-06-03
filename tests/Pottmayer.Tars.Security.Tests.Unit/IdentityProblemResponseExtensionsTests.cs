using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.Security.Identity.AspNetCore.DI;
using Pottmayer.Tars.Web.Http;
using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class IdentityProblemResponseExtensionsTests
{
    [Fact]
    public async Task OnForbidden_writes_403_envelope_from_mapper()
    {
        var (httpContext, options) = Build(forbiddenError: Error.Forbidden("Identity.AccessDenied", "Access denied."));
        var context = new ForbiddenContext(httpContext, Scheme(), options);

        await options.Events!.OnForbidden(context);

        httpContext.Response.StatusCode.Should().Be(403);

        var body = await ReadBody(httpContext);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("errorCode").GetString().Should().Be("Identity.AccessDenied");
        body.GetProperty("errorMessage").GetString().Should().Be("Access denied.");
    }

    [Fact]
    public async Task OnChallenge_writes_401_envelope_from_mapper()
    {
        var (httpContext, options) = Build(unauthorizedError: Error.Unauthorized("Identity.NotAuthenticated", "Authentication required."));
        var context = new JwtBearerChallengeContext(httpContext, Scheme(), options, new AuthenticationProperties());

        await options.Events!.OnChallenge(context);

        context.Handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(401);

        var body = await ReadBody(httpContext);
        body.GetProperty("errorCode").GetString().Should().Be("Identity.NotAuthenticated");
    }

    [Fact]
    public void Configure_preserves_existing_message_received_handler()
    {
        var options = new JwtBearerOptions { Events = new JwtBearerEvents() };
        var existing = options.Events.OnMessageReceived = _ => Task.CompletedTask;

        options.ConfigureTarsIdentityProblemResponses();

        options.Events.OnMessageReceived.Should().BeSameAs(existing);
        options.Events.OnChallenge.Should().NotBeNull();
        options.Events.OnForbidden.Should().NotBeNull();
    }

    private static (HttpContext, JwtBearerOptions) Build(Error? unauthorizedError = null, Error? forbiddenError = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpErrorMapper, EchoHttpErrorMapper>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.Response.Body = new MemoryStream();

        var options = new JwtBearerOptions();
        options.ConfigureTarsIdentityProblemResponses(unauthorizedError, forbiddenError);

        return (httpContext, options);
    }

    private static AuthenticationScheme Scheme()
        => new("Bearer", "Bearer", typeof(JwtBearerHandler));

    private static async Task<JsonElement> ReadBody(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    private sealed class EchoHttpErrorMapper : IHttpErrorMapper
    {
        public int MapToStatusCode(ErrorType errorType) => 500;

        public IHttpErrorResponse Map(Error error) => new HttpErrorResponse
        {
            Success      = false,
            ErrorCode    = error.Code,
            ErrorMessage = error.Message
        };

        public IHttpErrorResponse Map(Exception exception) => new HttpErrorResponse
        {
            Success   = false,
            ErrorCode = "INTERNAL_SERVER_ERROR"
        };
    }
}
