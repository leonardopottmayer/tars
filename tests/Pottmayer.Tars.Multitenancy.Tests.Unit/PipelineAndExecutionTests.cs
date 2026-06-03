using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Multitenancy.Abstractions.Context;
using Pottmayer.Tars.Multitenancy.Abstractions.Execution;
using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;
using Pottmayer.Tars.Multitenancy.Catalog;
using Pottmayer.Tars.Multitenancy.Context;
using Pottmayer.Tars.Multitenancy.Execution;
using Pottmayer.Tars.Multitenancy.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Tests.Unit;

public class PipelineAndExecutionTests
{
    [Fact]
    public async Task Pipeline_returns_first_resolved_result()
    {
        var pipeline = new TenantResolverPipeline(
        [
            new NullTenantResolver(),
            new StaticTenantResolver("first"),
            new StaticTenantResolver("second"),
        ]);

        var result = await pipeline.ResolveAsync(new TenantResolutionRequest());

        result.IsResolved.Should().BeTrue();
        result.TenantKey.Should().Be("first");
    }

    [Fact]
    public async Task Pipeline_unresolved_when_no_resolver_matches()
    {
        var pipeline = new TenantResolverPipeline([new NullTenantResolver(), new NullTenantResolver()]);

        var result = await pipeline.ResolveAsync(new TenantResolutionRequest());

        result.IsResolved.Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryCatalog_lists_all_tenants()
    {
        var catalog = new InMemoryTenantCatalog(["a", "b", "c"]);

        var keys = new List<string?>();
        await foreach (var t in catalog.ListAsync())
            keys.Add(t.TenantKey);

        keys.Should().Equal("a", "b", "c");
    }

    [Fact]
    public async Task Runner_sets_ambient_context_during_work_and_restores_after()
    {
        var accessor = new TenantContextAccessor();
        var provider = new ServiceCollection().BuildServiceProvider();
        var runner = new TenantExecutionRunner(provider, accessor);
        string? observed = null;

        await runner.RunForTenantAsync(
            TenantContext.Create("acme"),
            (sp, ct) => { observed = accessor.Current?.TenantKey; return Task.CompletedTask; });

        observed.Should().Be("acme");
        accessor.Current.Should().BeNull();
    }

    [Fact]
    public async Task Runner_iterates_every_tenant()
    {
        var accessor = new TenantContextAccessor();
        var provider = new ServiceCollection().BuildServiceProvider();
        var runner = new TenantExecutionRunner(provider, accessor);
        var catalog = new InMemoryTenantCatalog(["a", "b"]);
        var seen = new List<string?>();

        await runner.RunForEachTenantAsync(
            catalog.ListAsync(),
            (sp, tenant, ct) => { lock (seen) seen.Add(tenant.TenantKey); return Task.CompletedTask; });

        seen.Should().BeEquivalentTo("a", "b");
    }
}
