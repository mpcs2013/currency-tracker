using CurrencyTracker.Api.IntegrationTests.Rates;

namespace CurrencyTracker.Api.IntegrationTests.Health;

/// <summary>
/// Proves the 13.8 liveness/readiness contract against a live Postgres and
/// Redis (via <see cref="RatesApiFixture"/>): liveness is up while the
/// process runs, readiness is up only when both stores are reachable.
/// </summary>
public sealed class HealthEndpointsTests(RatesApiFixture fixture) : IClassFixture<RatesApiFixture>
{
    [Fact]
    public async Task Live_returns_200_while_the_process_runs()
    {
        // Act / Assert
        await fixture.Host.Scenario(scenario =>
        {
            scenario.Get.Url("/health/live");
            scenario.StatusCodeShouldBe(200);
        });
    }

    [Fact]
    public async Task Ready_returns_200_when_postgres_and_redis_are_reachable()
    {
        // Act / Assert
        await fixture.Host.Scenario(scenario =>
        {
            scenario.Get.Url("/health/ready");
            scenario.StatusCodeShouldBe(200);
        });
    }
}
