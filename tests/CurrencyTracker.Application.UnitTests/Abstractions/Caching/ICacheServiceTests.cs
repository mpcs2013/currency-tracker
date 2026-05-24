using CurrencyTracker.Application.Abstractions.Caching;
using CurrencyTracker.Application.UnitTests.Fakes;

namespace CurrencyTracker.Application.UnitTests.Abstractions;

/// <summary>
/// Tests exercising the <see cref="ICacheService"/> contract via the
/// <see cref="InMemoryCacheService"/> fake: presence, absence, expiry,
/// removal, and the cache-aside <c>GetOrSetAsync</c> helper.
/// </summary>
public sealed class ICacheServiceTests
{
    private static readonly CancellationToken NoCt = CancellationToken.None;

    [Fact]
    public async Task GetAsync_returns_null_for_missing_key()
    {
        var sut = new InMemoryCacheService();

        var actual = await sut.GetAsync("missing", NoCt);

        actual.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_then_GetAsync_returns_the_value()
    {
        var sut = new InMemoryCacheService();

        await sut.SetAsync("k", "v", TimeSpan.FromMinutes(5), NoCt);
        var actual = await sut.GetAsync("k", NoCt);

        actual.Should().Be("v");
    }

    [Fact]
    public async Task SetAsync_with_zero_ttl_makes_value_immediately_expired()
    {
        var sut = new InMemoryCacheService();

        await sut.SetAsync("k", "v", TimeSpan.Zero, NoCt);
        var actual = await sut.GetAsync("k", NoCt);

        actual.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_deletes_the_value()
    {
        var sut = new InMemoryCacheService();
        await sut.SetAsync("k", "v", TimeSpan.FromMinutes(5), NoCt);

        await sut.RemoveAsync("k", NoCt);
        var actual = await sut.GetAsync("k", NoCt);

        actual.Should().BeNull();
    }

    [Fact]
    public async Task GetOrSetAsync_returns_existing_value_without_calling_factory()
    {
        var sut = new InMemoryCacheService();
        await sut.SetAsync("k", "\"cached\"", TimeSpan.FromMinutes(5), NoCt);
        var factoryCalls = 0;

        var actual = await sut.GetOrSetAsync(
            "k",
            ct =>
            {
                factoryCalls++;
                return Task.FromResult("fresh");
            },
            TimeSpan.FromMinutes(5),
            NoCt
        );

        actual.Should().Be("cached");
        factoryCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetOrSetAsync_calls_factory_when_key_missing_and_caches_result()
    {
        var sut = new InMemoryCacheService();

        var first = await sut.GetOrSetAsync(
            "k",
            ct => Task.FromResult("fresh"),
            TimeSpan.FromMinutes(5),
            NoCt
        );
        var second = await sut.GetAsync("k", NoCt);

        first.Should().Be("fresh");
        second.Should().Be("\"fresh\"");
    }

    [Fact]
    public async Task GetOrSetAsync_propagates_cancellation_through_factory()
    {
        var sut = new InMemoryCacheService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () =>
            await sut.GetOrSetAsync(
                "k",
                ct =>
                {
                    ct.ThrowIfCancellationRequested();
                    return Task.FromResult("fresh");
                },
                TimeSpan.FromMinutes(5),
                cts.Token
            );

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetAsync_returns_null_for_expired_value()
    {
        var ct = TestContext.Current?.CancellationToken ?? CancellationToken.None;
        var sut = new InMemoryCacheService();
        await sut.SetAsync("k", "v", TimeSpan.FromMilliseconds(10), ct);

        await Task.Delay(50, ct);
        var actual = await sut.GetAsync("k", ct);

        actual.Should().BeNull();
    }
}
