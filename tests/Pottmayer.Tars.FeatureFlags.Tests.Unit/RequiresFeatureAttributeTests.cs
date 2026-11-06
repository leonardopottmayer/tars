using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.FeatureFlags.AspNetCore.Attributes;
using Pottmayer.Tars.FeatureFlags.DI;
using Pottmayer.Tars.Web.Http;
using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.FeatureFlags.Tests.Unit;

public class RequiresFeatureAttributeTests
{
    private static (ActionExecutingContext Context, InMemoryFeatureFlagStore Store) CreateContext()
    {
        var mapper = new Mock<IHttpErrorMapper>();
        mapper.Setup(m => m.MapToStatusCode(ErrorType.NotFound)).Returns(404);
        mapper.Setup(m => m.Map(It.IsAny<Error>()))
            .Returns((Error e) => new HttpErrorResponse { Success = false, ErrorCode = e.Code, ErrorMessage = e.Message });

        var services = new ServiceCollection();
        services.AddTarsFeatureFlagDefinitions(f => f.Add("finances.import"));
        services.AddTarsFeatureFlagCatalog();
        services.AddTarsFeatureFlagService();
        services.AddTarsInMemoryFeatureFlagStore();
        services.AddSingleton(mapper.Object);
        var sp = services.BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = sp };
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(action, [], new Dictionary<string, object?>(), controller: new object());
        return (context, sp.GetRequiredService<InMemoryFeatureFlagStore>());
    }

    [Fact]
    public async Task Enabled_feature_runs_the_action()
    {
        var (context, _) = CreateContext();
        var ran = false;

        await new RequiresFeatureAttribute("finances.import").OnActionExecutionAsync(context, () =>
        {
            ran = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        ran.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task Disabled_feature_returns_404_with_the_feature_disabled_code()
    {
        var (context, store) = CreateContext();
        store.Set("finances.import", false);
        var ran = false;

        await new RequiresFeatureAttribute("finances.import").OnActionExecutionAsync(context, () =>
        {
            ran = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        ran.Should().BeFalse();
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(404);
        result.Value.Should().BeOfType<HttpErrorResponse>().Which.ErrorCode.Should().Be("feature.disabled");
    }
}
