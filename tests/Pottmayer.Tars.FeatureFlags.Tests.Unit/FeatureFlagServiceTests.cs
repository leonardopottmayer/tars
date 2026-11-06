using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.FeatureFlags.Abstractions;
using Pottmayer.Tars.FeatureFlags.Configuration;
using Pottmayer.Tars.FeatureFlags.DI;

namespace Pottmayer.Tars.FeatureFlags.Tests.Unit;

public class FeatureFlagServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ServiceProvider Build(params Action<FeatureFlagCatalogConfiguration>[] modules)
    {
        var services = new ServiceCollection();
        foreach (var declare in modules)
            services.AddTarsFeatureFlagDefinitions(declare);
        services.AddTarsFeatureFlagCatalog();
        services.AddTarsFeatureFlagService();
        services.AddTarsInMemoryFeatureFlagStore();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildFinances() => Build(
        f => f.Add("finances").Add("finances.import", parent: "finances"),
        f => f.Add("assistant.chat", defaultOn: false));

    [Fact]
    public async Task Default_applies_while_no_override_is_stored()
    {
        using var sp = BuildFinances();
        var flags = sp.GetRequiredService<IFeatureFlags>();

        (await flags.IsEnabledAsync("finances.import", Ct)).Should().BeTrue();
        (await flags.IsEnabledAsync("assistant.chat", Ct)).Should().BeFalse();
    }

    [Fact]
    public async Task Override_wins_over_the_default_once_invalidated()
    {
        using var sp = BuildFinances();
        var flags = sp.GetRequiredService<IFeatureFlags>();
        var store = sp.GetRequiredService<InMemoryFeatureFlagStore>();
        (await flags.IsEnabledAsync("assistant.chat", Ct)).Should().BeFalse();

        store.Set("assistant.chat", true);
        (await flags.IsEnabledAsync("assistant.chat", Ct)).Should().BeFalse("the overrides are cached");

        flags.Invalidate();
        (await flags.IsEnabledAsync("assistant.chat", Ct)).Should().BeTrue();
    }

    [Fact]
    public async Task Child_is_off_while_its_parent_is_off()
    {
        using var sp = BuildFinances();
        sp.GetRequiredService<InMemoryFeatureFlagStore>().Set("finances", false);
        var flags = sp.GetRequiredService<IFeatureFlags>();

        (await flags.IsEnabledAsync("finances.import", Ct)).Should().BeFalse();
    }

    [Fact]
    public async Task Undeclared_key_throws_and_names_the_fix()
    {
        using var sp = BuildFinances();
        var flags = sp.GetRequiredService<IFeatureFlags>();

        var act = async () => await flags.IsEnabledAsync("finances.imprt", Ct);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*finances.imprt*AddTarsFeatureFlagDefinitions*");
    }

    [Fact]
    public async Task GetAll_reports_override_and_effective_state_and_ignores_undeclared_overrides()
    {
        using var sp = BuildFinances();
        var store = sp.GetRequiredService<InMemoryFeatureFlagStore>();
        store.Set("finances", false);
        store.Set("removed.flag", true);

        var states = await sp.GetRequiredService<IFeatureFlags>().GetAllAsync(Ct);

        states.Select(s => (s.Definition.Key, s.Override, s.Enabled)).Should().Equal(
            ("finances", false, false),
            ("finances.import", null, false),
            ("assistant.chat", null, false));
    }

    [Fact]
    public async Task Scoped_store_is_resolved_from_its_own_scope()
    {
        var services = new ServiceCollection();
        services.AddTarsFeatureFlagDefinitions(f => f.Add("notes"));
        services.AddTarsFeatureFlagCatalog();
        services.AddTarsFeatureFlagService();
        services.AddScoped<IFeatureFlagStore, OffStore>();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        (await sp.GetRequiredService<IFeatureFlags>().IsEnabledAsync("notes", Ct)).Should().BeFalse();
    }

    [Theory]
    [InlineData("duplicate", "*'a' is declared more than once*")]
    [InlineData("missing-parent", "*'b' has parent 'nope'*")]
    [InlineData("cycle", "*parent cycle*")]
    [InlineData("blank", "*blank key*")]
    public void Invalid_catalog_fails_with_the_rule_in_the_message(string scenario, string message)
    {
        Action<FeatureFlagCatalogConfiguration> declare = scenario switch
        {
            "duplicate" => f => f.Add("a").Add("a"),
            "missing-parent" => f => f.Add("b", parent: "nope"),
            "cycle" => f => f.Add("x", parent: "y").Add("y", parent: "x"),
            _ => f => f.Add(" "),
        };
        using var sp = Build(declare);

        var act = () => sp.GetRequiredService<IFeatureFlagCatalog>();

        act.Should().Throw<OptionsValidationException>().WithMessage(message);
    }

    private sealed class OffStore : IFeatureFlagStore
    {
        public Task<IReadOnlyDictionary<string, bool>> GetOverridesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, bool>>(new Dictionary<string, bool> { ["notes"] = false });
    }
}
