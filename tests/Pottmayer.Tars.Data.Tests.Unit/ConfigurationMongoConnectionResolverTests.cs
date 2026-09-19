using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Data.Document.Abstractions.Connection;
using Pottmayer.Tars.Data.Document.MongoDB.Connection;

namespace Pottmayer.Tars.Data.Tests.Unit;

/// <summary>
/// Mongo connection resolution mirrors the relational precedence (tenant-specific → template → static),
/// but resolves a database <b>name</b> instead of a <c>DbProvider</c>.
/// </summary>
public class ConfigurationMongoConnectionResolverTests
{
    private static readonly IServiceProvider Services = new ServiceCollection().BuildServiceProvider();

    private static ConfigurationMongoConnectionResolver Resolver(Dictionary<string, string?> settings)
        => new(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

    private static MongoConnectionResolutionContext Context(string key, string? tenant = null)
        => new() { DatabaseKey = key, TenantKey = tenant, ServiceProvider = Services };

    [Fact]
    public async Task Resolves_static_connection_with_database_name()
    {
        var resolver = Resolver(new()
        {
            ["Tars:Data:Mongo:Connections:catalog:ConnectionString"] = "mongodb://localhost:27017",
            ["Tars:Data:Mongo:Connections:catalog:Database"] = "catalog_db",
        });

        var descriptor = await resolver.ResolveAsync(Context("catalog"));

        descriptor.Should().NotBeNull();
        descriptor!.ConnectionString.Should().Be("mongodb://localhost:27017");
        descriptor.DatabaseName.Should().Be("catalog_db");
        descriptor.IsTenantScoped.Should().BeFalse();
    }

    [Fact]
    public async Task Tenant_specific_connection_wins()
    {
        var resolver = Resolver(new()
        {
            ["Tars:Data:Mongo:TenantConnections:primary:acme:ConnectionString"] = "mongodb://acme",
            ["Tars:Data:Mongo:TenantConnections:primary:acme:Database"] = "acme_db",
            ["Tars:Data:Mongo:Connections:primary:ConnectionString"] = "mongodb://static",
            ["Tars:Data:Mongo:Connections:primary:Database"] = "static_db",
        });

        var descriptor = await resolver.ResolveAsync(Context("primary", tenant: "acme"));

        descriptor!.ConnectionString.Should().Be("mongodb://acme");
        descriptor.DatabaseName.Should().Be("acme_db");
        descriptor.IsTenantScoped.Should().BeTrue();
        descriptor.TenantKey.Should().Be("acme");
    }

    [Fact]
    public async Task Template_expands_tenant_placeholders_in_string_and_database()
    {
        var resolver = Resolver(new()
        {
            ["Tars:Data:Mongo:TenantConnectionTemplates:primary:Template"] = "mongodb://{tenantKey}-host",
            ["Tars:Data:Mongo:TenantConnectionTemplates:primary:Database"] = "{tenantKey}_primary",
        });

        var descriptor = await resolver.ResolveAsync(Context("primary", tenant: "globex"));

        descriptor!.ConnectionString.Should().Be("mongodb://globex-host");
        descriptor.DatabaseName.Should().Be("globex_primary");
        descriptor.IsTenantScoped.Should().BeTrue();
    }

    [Fact]
    public async Task Falls_back_to_static_when_tenant_has_no_specific_or_template()
    {
        var resolver = Resolver(new()
        {
            ["Tars:Data:Mongo:Connections:primary:ConnectionString"] = "mongodb://shared",
            ["Tars:Data:Mongo:Connections:primary:Database"] = "shared_db",
        });

        var descriptor = await resolver.ResolveAsync(Context("primary", tenant: "acme"));

        descriptor!.ConnectionString.Should().Be("mongodb://shared");
        descriptor.DatabaseName.Should().Be("shared_db");
    }

    [Fact]
    public async Task Returns_null_when_nothing_configured()
    {
        var resolver = Resolver(new());

        (await resolver.ResolveAsync(Context("unknown"))).Should().BeNull();
    }

    [Fact]
    public async Task Missing_database_name_throws()
    {
        var resolver = Resolver(new()
        {
            ["Tars:Data:Mongo:Connections:catalog:ConnectionString"] = "mongodb://localhost:27017",
        });

        var act = async () => await resolver.ResolveAsync(Context("catalog"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
