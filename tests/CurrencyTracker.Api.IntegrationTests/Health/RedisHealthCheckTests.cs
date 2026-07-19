using CurrencyTracker.Api.Health;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace CurrencyTracker.Api.IntegrationTests.Health;

/// <summary>
/// Unit-level coverage of the <see cref="RedisHealthCheck"/> seam: a
/// responsive cache yields Healthy. The unreachable path (GetAsync throws →
/// framework reports Unhealthy) is proven end-to-end in
/// <see cref="HealthReadinessFlipTests"/>.
/// </summary>
public sealed class RedisHealthCheckTests
{
    [Fact]
    public async Task Returns_healthy_when_the_cache_responds()
    {
        // Arrange
        var cache = Substitute.For<IDistributedCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        var check = new RedisHealthCheck(cache);

        // Act
        var result = await check.CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
