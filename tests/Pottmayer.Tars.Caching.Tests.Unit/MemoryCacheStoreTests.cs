using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Pottmayer.Tars.Caching;
using Pottmayer.Tars.Caching.Memory;

namespace Pottmayer.Tars.Caching.Tests.Unit;

public class MemoryCacheStoreTests
{
    private static MemoryCacheStore Create()
    {
        var keys = new DefaultCacheKeyBuilder(new TestCachingOptions { KeyPrefix = "t", KeySeparator = ":" });
        return new MemoryCacheStore(new MemoryCache(new MemoryCacheOptions()), keys);
    }

    [Fact]
    public async Task Set_then_Get_returns_value()
    {
        var store = Create();

        await store.SetAsync("k", 123, ct: TestContext.Current.CancellationToken);

        (await store.GetAsync<int>("k", TestContext.Current.CancellationToken)).Should().Be(123);
    }

    [Fact]
    public async Task Get_missing_returns_default()
    {
        var store = Create();

        (await store.GetAsync<int>("missing", TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task TryGet_reports_found_state()
    {
        var store = Create();
        await store.SetAsync("k", "v", ct: TestContext.Current.CancellationToken);

        var hit = await store.TryGetAsync<string>("k", TestContext.Current.CancellationToken);
        var miss = await store.TryGetAsync<string>("nope", TestContext.Current.CancellationToken);

        hit.Found.Should().BeTrue();
        hit.Value.Should().Be("v");
        miss.Found.Should().BeFalse();
    }

    [Fact]
    public async Task Remove_and_Exists_behave_consistently()
    {
        var store = Create();
        await store.SetAsync("k", "v", ct: TestContext.Current.CancellationToken);

        (await store.ExistsAsync("k", TestContext.Current.CancellationToken)).Should().BeTrue();
        await store.RemoveAsync("k", TestContext.Current.CancellationToken);
        (await store.ExistsAsync("k", TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task GetOrSet_invokes_factory_only_on_miss()
    {
        var store = Create();
        var calls = 0;

        var first = await store.GetOrSetAsync("k", _ => { calls++; return Task.FromResult(99); }, ct: TestContext.Current.CancellationToken);
        var second = await store.GetOrSetAsync("k", _ => { calls++; return Task.FromResult(-1); }, ct: TestContext.Current.CancellationToken);

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
