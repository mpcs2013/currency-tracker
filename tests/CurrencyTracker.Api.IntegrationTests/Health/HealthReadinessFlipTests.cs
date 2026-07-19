using Alba;
using CurrencyTracker.Api.IntegrationTests.Auth;
using Testcontainers.PostgreSql;

namespace CurrencyTracker.Api.IntegrationTests.Health;

/// <summary>
/// Boots the Api with Postgres reachable but Redis pointed at a dead port,
/// proving readiness flips to 503 while liveness stays 200.
/// </summary>
public sealed class HealthReadinessFlipTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("currencytracker")
        .Build();

    private IAlbaHost _host = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        _host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:currencytracker",
                _postgres.GetConnectionString()
            );
            // Nothing listening on 6390 → the Redis readiness probe's GetAsync throws.
            builder.UseSetting(
                "ConnectionStrings:cache",
                "127.0.0.1:6390,connectTimeout=250,connectRetry=1,abortConnect=false"
            );
            builder.UseSetting(
                "Authentication:Authority",
                "https://test.local/realms/currency-tracker"
            );
            builder.UseSetting("Authentication:Audience", "currency-tracker-api");
            builder.UseTestJwtBearer();
            builder.UseStubExchangeRateProvider();
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Ready_flips_to_503_when_redis_is_unreachable()
    {
        // Act / Assert
        await _host.Scenario(scenario =>
        {
            scenario.Get.Url("/health/ready");
            scenario.StatusCodeShouldBe(503);
        });
    }

    [Fact]
    public async Task Live_stays_200_when_redis_is_unreachable()
    {
        // Act / Assert
        await _host.Scenario(scenario =>
        {
            scenario.Get.Url("/health/live");
            scenario.StatusCodeShouldBe(200);
        });
    }
}
