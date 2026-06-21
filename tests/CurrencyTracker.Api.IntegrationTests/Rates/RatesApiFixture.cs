using Alba;
using CurrencyTracker.Api.IntegrationTests.Auth;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Rates;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace CurrencyTracker.Api.IntegrationTests.Rates;

/// <summary>
/// Boots the Api (Alba) against a Testcontainers Postgres and Redis, with
/// the two connection strings overridden to point at the containers.
/// </summary>
public sealed class RatesApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18") // pin major to match the AppHost (Phase 7); image goes in the ctor (4.12.0)
        .WithDatabase("currencytracker")
        .Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7.4").Build(); // image in the ctor; parameterless ctor is obsolete in 4.12.0

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
            // Development env so the dev MigrationRunner applies migrations to the container.
            builder.UseSetting(
                "Authentication:Authority",
                "https://test.local/realms/currency-tracker"
            );
            builder.UseSetting("Authentication:Audience", "currency-tracker-api");
            builder.UseTestJwtBearer(); // ← 11.7: trust TestJwt's signing key
        });
    }

    public async ValueTask DisposeAsync()
    {
        await Host.DisposeAsync();
        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // Helper for tests to seed/mutate the DB directly.
    public async Task SeedSnapshotAsync(RateSnapshot snapshot)
    {
        using var scope = Host.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IExchangeRateRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repo.SaveSnapshotAsync(snapshot, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
    }
}
