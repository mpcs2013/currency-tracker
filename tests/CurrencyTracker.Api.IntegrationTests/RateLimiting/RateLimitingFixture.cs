using Alba;
using CurrencyTracker.Api.IntegrationTests.Auth;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace CurrencyTracker.Api.IntegrationTests.RateLimiting;

/// <summary>
/// Boots the Api with deliberately tiny rate limits (3 permits / 3 tokens)
/// so a short request loop deterministically trips the limiter.
/// </summary>
public sealed class RateLimitingFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("currencytracker")
        .Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7.4").Build();

    public IAlbaHost Host { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:currencytracker",
                _postgres.GetConnectionString()
            );
            builder.UseSetting("ConnectionStrings:cache", _redis.GetConnectionString());
            builder.UseSetting(
                "Authentication:Authority",
                "https://test.local/realms/currency-tracker"
            );
            builder.UseSetting("Authentication:Audience", "currency-tracker-api");
            builder.UseTestJwtBearer();
            builder.UseStubExchangeRateProvider();

            // Tiny limits so 4 quick requests trip the per-IP window.
            builder.UseSetting("RateLimiting:PerIp:PermitLimit", "3");
            builder.UseSetting("RateLimiting:PerIp:WindowSeconds", "60");
            builder.UseSetting("RateLimiting:PerUser:TokenLimit", "3");
            builder.UseSetting("RateLimiting:PerUser:TokensPerPeriod", "1");
            builder.UseSetting("RateLimiting:PerUser:ReplenishmentPeriodSeconds", "60");
        });
    }

    public async ValueTask DisposeAsync()
    {
        await Host.DisposeAsync();
        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
