using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Pottmayer.Tars.Caching.Abstractions;
using Pottmayer.Tars.Caching.Core;
using Pottmayer.Tars.Caching.Core.Options;
using Pottmayer.Tars.Caching.Memory;

namespace Pottmayer.Tars.Caching.Tests.Unit;

public class MemoryCacheStoreTests
{
    private static MemoryCacheStore Create()
    {
        var monitor = new Mock<IOptionsMonitor<CacheOptions>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(new CacheOptions { KeyPrefix = "t", KeySeparator = ":" });
        var keys = new DefaultCacheKeyBuilder(monitor.Object);
        return new MemoryCacheStore(new MemoryCache(new MemoryCacheOptions()), keys);
    }

    [Fact]
    public async Task Set_then_Get_returns_value()
    {
        var store = Create();

        await store.SetAsync("k", 123);

        (await store.GetAsync<int>("k")).Should().Be(123);
    }

    [Fact]
    public async Task Get_missing_returns_default()
    {
        var store = Create();

        (await store.GetAsync<int>("missing")).Should().Be(0);
    }

    [Fact]
    public async Task TryGet_reports_found_state()
    {
        var store = Create();
        await store.SetAsync("k", "v");

        var hit = await store.TryGetAsync<string>("k");
        var miss = await store.TryGetAsync<string>("nope");

        hit.Found.Should().BeTrue();
        hit.Value.Should().Be("v");
        miss.Found.Should().BeFalse();
    }

    [Fact]
    public async Task Remove_and_Exists_behave_consistently()
    {
        var store = Create();
        await store.SetAsync("k", "v");

        (await store.ExistsAsync("k")).Should().BeTrue();
        await store.RemoveAsync("k");
        (await store.ExistsAsync("k")).Should().BeFalse();
    }

    [Fact]
    public async Task GetOrSet_invokes_factory_only_on_miss()
    {
        var store = Create();
        var calls = 0;

        var first = await store.GetOrSetAsync("k", _ => { calls++; return Task.FromResult(99); });
        var second = await store.GetOrSetAsync("k", _ => { calls++; return Task.FromResult(-1); });

        first.Should().Be(99);
        second.Should().Be(99);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task Cancellation_is_observed()
    {
        var store = Create();
        var cancelled = new CancellationToken(canceled: true);

        var act = async () => await store.SetAsync("k", 1, options: null, ct: cancelled);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
