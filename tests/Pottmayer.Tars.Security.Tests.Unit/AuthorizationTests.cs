using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.FeatureFlags;
using Pottmayer.Tars.FeatureFlags.Abstractions;
using Pottmayer.Tars.FeatureFlags.DI;
using Pottmayer.Tars.Security.Authorization;
using Pottmayer.Tars.Security.Authorization.Abstractions;
using Pottmayer.Tars.Security.Authorization.AspNetCore.DI;
using Pottmayer.Tars.Security.Authorization.AspNetCore.Filters;
using Pottmayer.Tars.Security.Authorization.Configuration;
using Pottmayer.Tars.Security.Authorization.DI;
using Pottmayer.Tars.Web.Http;
using Pottmayer.Tars.Web.Http.Abstractions;
using System.Security.Claims;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class AuthorizationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static void DeclareFinances(PermissionCatalogConfiguration p) => p
        .Module("finances", feature: "finances")
        .Resource("cards").Crud()
        .Resource("imports", feature: "finances.import").Actions("run", "read")
        .Resource("transactions").Crud().Actions("approve");

    private static ServiceProvider Build(
        FakeStore store,
        Action<PermissionCatalogConfiguration>? declare = null,
        Action<IServiceCollection>? extra = null)
    {
        var services = new ServiceCollection();
        services.AddTarsFeatureFlagDefinitions(f => f.Add("finances").Add("finances.import", parent: "finances"));
        services.AddTarsFeatureFlagCatalog();
        services.AddTarsFeatureFlagService();
        services.AddTarsInMemoryFeatureFlagStore();
        services.AddTarsPermissionDefinitions(declare ?? DeclareFinances);
        services.AddTarsPermissionCatalog();
        services.AddTarsPermissionChecker();
        services.AddTarsAuthorizationGate();
        services.AddSingleton<IAuthorizationStore>(store);
        extra?.Invoke(services);
        return services.BuildServiceProvider();
    }

    // ---- catalog ------------------------------------------------------------------------------

    [Fact]
    public void Catalog_expands_crud_and_actions_with_read_dependencies_and_features()
    {
        using var sp = Build(new FakeStore());
        var catalog = sp.GetRequiredService<IPermissionCatalog>();

        catalog.Definitions.Select(d => d.Key).Should().Equal(
            "finances.cards.read", "finances.cards.create", "finances.cards.update", "finances.cards.delete",
            "finances.imports.run", "finances.imports.read",
            "finances.transactions.read", "finances.transactions.create", "finances.transactions.update",
            "finances.transactions.delete", "finances.transactions.approve");

        catalog.Find("finances.cards.read")!.Requires.Should().BeEmpty();
        catalog.Find("finances.imports.run")!.Requires.Should().Equal("finances.imports.read");
        catalog.Find("finances.imports.run")!.Feature.Should().Be("finances.import");
        catalog.Find("finances.cards.delete")!.Feature.Should().Be("finances");
    }

    [Fact]
    public void Single_action_shortcuts_declare_the_standard_actions()
    {
        using var sp = Build(new FakeStore(), p => p.Module("notes").Resource("pages").Read().Create().Update().Delete());

        sp.GetRequiredService<IPermissionCatalog>().Definitions.Select(d => d.Key).Should().Equal(
            "notes.pages.read", "notes.pages.create", "notes.pages.update", "notes.pages.delete");
    }

    [Theory]
    [InlineData("duplicate", "*'notes.pages.read' is declared more than once*")]
    [InlineData("segment", "*'notes.pa.ges.read' has a blank segment*")]
    [InlineData("feature", "*Feature flag 'notes' gates permissions but is not declared*")]
    [InlineData("usage", "*requires permission 'nope.nothing.here'*")]
    public void Invalid_catalog_fails_with_the_rule_in_the_message(string scenario, string message)
    {
        Action<PermissionCatalogConfiguration> declare = scenario switch
        {
            "duplicate" => p => p.Module("notes").Resource("pages").Actions("read", "read"),
            "segment" => p => p.Module("notes").Resource("pa.ges").Actions("read"),
            "feature" => p => p.Module("notes", feature: "notes").Resource("pages").Actions("read"),
            _ => p => DeclareFinances(p.CheckUsagesIn(typeof(AuthorizationTests).Assembly)),
        };
        using var sp = Build(new FakeStore(), declare);

        var act = () => sp.GetRequiredService<IPermissionCatalog>();

        act.Should().Throw<OptionsValidationException>().WithMessage(message);
    }

    // ---- patterns and checker -----------------------------------------------------------------

    [Theory]
    [InlineData("*", "finances.cards.read", true)]
    [InlineData("finances.*", "finances.cards.read", true)]
    [InlineData("finances.cards.*", "finances.cards.read", true)]
    [InlineData("finances.cards.*", "finances.cardsx.read", false)]
    [InlineData("finances.cards.read", "finances.cards.read", true)]
    [InlineData("finances.cards.read", "finances.cards.delete", false)]
    public void Patterns_match_exact_keys_prefix_wildcards_and_star(string pattern, string key, bool expected)
        => PermissionPatterns.Matches(pattern, key).Should().Be(expected);

    [Fact]
    public async Task Effective_set_is_roles_plus_allows_minus_denies()
    {
        var store = new FakeStore { ["u1"] = new(["finances.cards.*"], ["finances.imports.read"], ["finances.cards.delete"]) };
        using var sp = Build(store);

        var effective = await sp.GetRequiredService<IPermissionChecker>().GetEffectiveAsync("u1", Ct);

        effective.Should().BeEquivalentTo(
            "finances.cards.read", "finances.cards.create", "finances.cards.update", "finances.imports.read");
    }

    [Fact]
    public async Task Deny_beats_star_and_unknown_patterns_are_ignored()
    {
        var store = new FakeStore { ["admin"] = new(["*", "removed.module.*"], [], ["finances.transactions.*"]) };
        using var sp = Build(store);
        var checker = sp.GetRequiredService<IPermissionChecker>();

        (await checker.HasAsync("admin", "finances.cards.delete", Ct)).Should().BeTrue();
        (await checker.HasAsync("admin", "finances.transactions.approve", Ct)).Should().BeFalse();
    }

    [Fact]
    public async Task Grants_are_cached_until_the_subject_is_invalidated()
    {
        var store = new FakeStore { ["u1"] = AuthorizationGrants.None };
        using var sp = Build(store);
        var checker = sp.GetRequiredService<IPermissionChecker>();
        (await checker.HasAsync("u1", "finances.cards.read", Ct)).Should().BeFalse();

        store["u1"] = new(["finances.*"], [], []);
        (await checker.HasAsync("u1", "finances.cards.read", Ct)).Should().BeFalse("the grants are cached");

        checker.Invalidate("u1");
        (await checker.HasAsync("u1", "finances.cards.read", Ct)).Should().BeTrue();

        store["u1"] = AuthorizationGrants.None;
        checker.InvalidateAll();
        (await checker.HasAsync("u1", "finances.cards.read", Ct)).Should().BeFalse();
    }

    [Fact]
    public async Task Undeclared_permission_throws_and_names_the_fix()
    {
        using var sp = Build(new FakeStore());

        var act = async () => await sp.GetRequiredService<IPermissionChecker>().HasAsync("u1", "finances.card.read", Ct);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*finances.card.read*AddTarsPermissionDefinitions*");
    }

    // ---- gate ---------------------------------------------------------------------------------

    [Fact]
    public async Task Gate_checks_the_feature_before_the_permission_even_for_star()
    {
        var store = new FakeStore { ["admin"] = new(["*"], [], []) };
        using var sp = Build(store);
        sp.GetRequiredService<InMemoryFeatureFlagStore>().Set("finances", false);
        var gate = sp.GetRequiredService<AuthorizationGate>();

        var error = await gate.CheckAsync("admin", ["finances.imports.run"], Ct);

        error!.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be(FeatureFlagErrors.Disabled);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("u1")]
    public async Task Gate_forbids_a_missing_subject_or_permission(string? subject)
    {
        var store = new FakeStore { ["u1"] = new(["finances.cards.read"], [], []) };
        using var sp = Build(store);

        var error = await sp.GetRequiredService<AuthorizationGate>().CheckAsync(subject, ["finances.cards.read", "finances.cards.delete"], Ct);

        error!.Type.Should().Be(ErrorType.Forbidden);
        error.Code.Should().Be(AuthorizationErrors.Denied);
    }

    [Fact]
    public async Task Gate_allows_when_every_permission_is_held()
    {
        var store = new FakeStore { ["u1"] = new(["finances.cards.*"], [], []) };
        using var sp = Build(store);

        (await sp.GetRequiredService<AuthorizationGate>().CheckAsync("u1", ["finances.cards.read", "finances.cards.delete"], Ct))
            .Should().BeNull();
    }

    // ---- mediator behavior --------------------------------------------------------------------

    private static ServiceProvider BuildWithBehavior(FakeStore store, bool withResolver = true) => Build(store, extra: s =>
    {
        s.AddTarsAuthorizationBehavior();
        if (withResolver)
            s.AddTarsAuthorizationActorResolver<UserIdActorResolver>();
    });

    private static async Task<(TResponse Response, bool Ran)> Send<TRequest, TResponse>(IServiceProvider sp, TRequest request, TResponse onRun)
        where TRequest : IRequest<TResponse>
    {
        using var scope = sp.CreateScope();
        var behavior = scope.ServiceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Single();
        var ran = false;
        var response = await behavior.Handle(request, () =>
        {
            ran = true;
            return ValueTask.FromResult(onRun);
        }, Ct);
        return (response, ran);
    }

    [Fact]
    public async Task Behavior_lets_unmarked_requests_through_without_any_resolver()
    {
        using var sp = BuildWithBehavior(new FakeStore(), withResolver: false);

        var (response, ran) = await Send(sp, new PingRequest(), Result.Success());

        ran.Should().BeTrue();
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Behavior_answers_a_denied_request_with_a_typed_failure_and_skips_the_handler()
    {
        var store = new FakeStore { ["u1"] = new(["finances.cards.read"], [], []) };
        using var sp = BuildWithBehavior(store);

        var (response, ran) = await Send(sp, new DeleteCardRequest("u1"), Result<string>.Success("deleted"));

        ran.Should().BeFalse();
        response.IsSuccess.Should().BeFalse();
        response.Errors.Single().Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Behavior_runs_the_handler_when_allowed()
    {
        var store = new FakeStore { ["u1"] = new(["finances.cards.*"], [], []) };
        using var sp = BuildWithBehavior(store);

        var (response, ran) = await Send(sp, new DeleteCardRequest("u1"), Result<string>.Success("deleted"));

        ran.Should().BeTrue();
        response.Value.Should().Be("deleted");
    }

    // ---- MVC filter ---------------------------------------------------------------------------

    [Fact]
    public async Task Mvc_filter_returns_403_for_a_denied_action_and_reads_the_subject_from_claims()
    {
        var mapper = new Mock<IHttpErrorMapper>();
        mapper.Setup(m => m.MapToStatusCode(ErrorType.Forbidden)).Returns(403);
        mapper.Setup(m => m.Map(It.IsAny<Error>()))
            .Returns((Error e) => new HttpErrorResponse { Success = false, ErrorCode = e.Code, ErrorMessage = e.Message });
        var store = new FakeStore { ["u1"] = new(["finances.cards.read"], [], []) };
        using var sp = Build(store, extra: s =>
        {
            s.AddSingleton(mapper.Object);
            s.AddTarsPermissionMvcFilter();
        });

        var http = new DefaultHttpContext
        {
            RequestServices = sp,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "u1")], "test")),
        };
        var descriptor = new ActionDescriptor { EndpointMetadata = [new RequiresPermissionAttribute("finances.cards.delete")] };
        var context = new ActionExecutingContext(
            new ActionContext(http, new RouteData(), descriptor), [], new Dictionary<string, object?>(), controller: new object());
        var ran = false;

        using var scope = sp.CreateScope();
        await scope.ServiceProvider.GetRequiredService<PermissionActionFilter>().OnActionExecutionAsync(context, () =>
        {
            ran = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        ran.Should().BeFalse();
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(403);
        result.Value.Should().BeOfType<HttpErrorResponse>().Which.ErrorCode.Should().Be(AuthorizationErrors.Denied);
    }

    // ---- fakes --------------------------------------------------------------------------------

    private sealed class FakeStore : Dictionary<string, AuthorizationGrants>, IAuthorizationStore
    {
        public Task<AuthorizationGrants> GetGrantsAsync(string subjectId, CancellationToken cancellationToken = default)
            => Task.FromResult(TryGetValue(subjectId, out var grants) ? grants : AuthorizationGrants.None);
    }

    private sealed record PingRequest : IRequest<Result>;

    [RequiresPermission("finances.cards.delete")]
    private sealed record DeleteCardRequest(string UserId) : IRequest<Result<string>>;

    [RequiresPermission("nope.nothing.here")]
    private sealed class UndeclaredUsage;

    private sealed class UserIdActorResolver : IAuthorizationActorResolver
    {
        public string? Resolve(object request) => (request as DeleteCardRequest)?.UserId;
    }
}
