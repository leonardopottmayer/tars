using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;
using Pottmayer.Tars.Data.Relational.Abstractions.Enums;
using Pottmayer.Tars.Data.Relational.DataConnection;

namespace Pottmayer.Tars.Data.Tests.Unit;

public class ConfigurationDataConnectionResolverTests
{
    private static readonly IServiceProvider Services = new ServiceCollection().BuildServiceProvider();

    private static ConfigurationDataConnectionResolver Resolver(Dictionary<string, string?> settings)
        => new(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

    private static DataConnectionResolutionContext Context(string key, string? tenant = null)
        => new() { DatabaseKey = key, TenantKey = tenant, ServiceProvider = Services };

    [Fact]
    public async Task Resolves_static_connection_when_no_tenant()
    {
        var resolver = Resolver(new()
        {
            ["Tars:Data:Connections:central:ConnectionString"] = "Host=central",
            ["Tars:Data:Connections:central:Provider"] = "PostgreSQL",
        });

        var descriptor = await resolver.ResolveAsync(Context("central"));

        descriptor.Should().NotBeNull();
        descriptor!.ConnectionString.Should().Be("Host=central");
        descriptor.Provider.Should().Be(DbProvider.PostgreSQL);
        descriptor.IsTenantScoped.Should().BeFalse();
    }

    [Fact]
    public async Task Tenant_specific_connection_wins_over_static_and_template()
    {
        var resolver = Resolver(new()
        {
            ["Tars:Data:TenantConnections:primary:acme:ConnectionString"] = "Host=acme-primary",
            ["Tars:Data:TenantConnectionTemplates:primary:Template"] = "Host={tenantKey}-tmpl",
            ["Tars:Data:Connections:primary:ConnectionString"] = "Host=static",
        });

        var descriptor = await resolver.ResolveAsync(Context("primary", tenant: "acme"));

        descriptor!.ConnectionString.Should().Be("Host=acme-primary");
        descriptor.IsTenantScoped.Should().BeTrue();
        descriptor.TenantKey.Should().Be("acme");
    }

    [Fact]
    public async Task Template_is_expanded_when_no_tenant_specific_entry()
    {
        var resolver = Resolver(new()
        {
            ["Tars:Data:TenantConnectionTemplates:primary:Template"] = "Host={tenantKey}-db",
            ["Tars:Data:TenantConnectionTemplates:primary:Provider"] = "SqlServer",
            ["Tars:Data:Connections:primary:ConnectionString"] = "Host=static",
        });

        var descriptor = await resolver.ResolveAsync(Context("primary", tenant: "globex"));

        descriptor!.ConnectionString.Should().Be("Host=globex-db");
        descriptor.Provider.Should().Be(DbProvider.SqlServer);
        descriptor.IsTenantScoped.Should().BeTrue();
    }

    [Fact]
    public async Task Falls_back_to_static_when_tenant_has_no_specific_or_template()
    {
        var resolver = Resolver(new()
        {
            ["Tars:Data:Connections:primary:ConnectionString"] = "Host=shared",
        });

        var descriptor = await resolver.ResolveAsync(Context("primary", tenant: "acme"));

        descriptor!.ConnectionString.Should().Be("Host=shared");
    }

    [Fact]
    public async Task Returns_null_when_nothing_configured()
    {
        var resolver = Resolver(new());

        (await resolver.ResolveAsync(Context("unknown"))).Should().BeNull();
    }

    [Fact]
    public async Task Unknown_provider_string_maps_to_Unknown()
    {
        var resolver = Resolver(new()
        {
            ["Tars:Data:Connections:default:ConnectionString"] = "Host=x",
            ["Tars:Data:Connections:default:Provider"] = "Cassandra",
        });

        var descriptor = await resolver.ResolveAsync(Context("default"));

        descriptor!.Provider.Should().Be(DbProvider.Unknown);
    }
}
