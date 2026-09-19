using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.DI;
using Pottmayer.Tars.Data.Tests.Unit.Infrastructure;

namespace Pottmayer.Tars.Data.Tests.Unit;

/// <summary>
/// Proves the coexistence seam: several providers register keyed factories under different keys and the
/// single composite <see cref="IDataContextFactory"/> routes each key to its own provider — the mechanism
/// that lets an app mix relational and document databases (e.g. 2 SQL + 1 Mongo) behind one entry point.
/// </summary>
public class CompositeDataContextFactoryTests
{
    private static IDataContextFactory Build(params (string key, string label)[] pipelines)
    {
        var services = new ServiceCollection().AddTarsDataContextFactory();
        foreach (var (key, label) in pipelines)
            services.AddScoped<IKeyedDataContextFactory>(_ => new FakeKeyedDataContextFactory(key, label));
        return services.BuildServiceProvider().GetRequiredService<IDataContextFactory>();
    }

    [Fact]
    public async Task Routes_each_key_to_its_own_provider()
    {
        var factory = Build(
            ("sql-central", "relational"),
            ("sql-primary", "relational"),
            ("mongo-catalog", "document"));

        var central = (FakeDataContext)await factory.CreateIsolatedAsync("sql-central");
        var primary = (FakeDataContext)await factory.CreateIsolatedAsync("sql-primary");
        var catalog = (FakeDataContext)await factory.CreateIsolatedAsync("mongo-catalog");

        central.Label.Should().Be("relational");
        primary.Label.Should().Be("relational");
        catalog.Label.Should().Be("document");
    }

    [Fact]
    public async Task Two_mongo_and_one_sql_all_resolve()
    {
        var factory = Build(
            ("mongo-a", "document"),
            ("mongo-b", "document"),
            ("sql", "relational"));

        (await factory.CreateIsolatedAsync("mongo-a")).Should().BeOfType<FakeDataContext>();
        (await factory.CreateIsolatedAsync("mongo-b")).Should().BeOfType<FakeDataContext>();
        (await factory.CreateIsolatedAsync("sql")).Should().BeOfType<FakeDataContext>();
    }

    [Fact]
    public async Task Unknown_key_throws_with_guidance()
    {
        var factory = Build(("sql", "relational"));

        var act = async () => await factory.CreateScopedAsync("missing");

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("missing");
    }

    [Fact]
    public void Routing_is_case_insensitive_on_key()
    {
        var factory = Build(("Mongo-Catalog", "document"));

        var act = async () => await factory.CreateIsolatedAsync("mongo-catalog");

        act.Should().NotThrowAsync();
    }
}
