using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;
using CurrencyTracker.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CurrencyTracker.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Real-Postgres round trip for the <see cref="EfExchangeRateRepository"/>.
/// Confirms that the Phase 8.3 configuration — composite key on
/// <c>rate_snapshots</c>, owned-type collection on <c>exchange_rates</c>,
/// <see cref="CurrencyCode"/> value-conversion — survives the
/// write-then-read cycle against Postgres.
/// </summary>
public sealed class ExchangeRateRoundTripTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initialises a new instance of <see cref="ExchangeRateRoundTripTests"/>.
    /// </summary>
    /// <param name="fixture">The shared Postgres fixture.</param>
    public ExchangeRateRoundTripTests(PostgresFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_fixture.ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options
        );

    [Fact]
    public async Task ExchangeRateRoundTrip_save_then_get_returns_equivalent_snapshot()
    {
        // Arrange
        var usd = CurrencyCode.Create("USD").Value;
        var eur = CurrencyCode.Create("EUR").Value;
        var gbp = CurrencyCode.Create("GBP").Value;
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var snapshot = RateSnapshot
            .Create(
                usd,
                asOf,
                [
                    ExchangeRate.Create(usd, eur, 0.92m, asOf).Value,
                    ExchangeRate.Create(usd, gbp, 0.79m, asOf).Value,
                ]
            )
            .Value;

        // Act: persist through one context.
        await using (var writeCtx = NewContext())
        {
            var writeRepo = new EfExchangeRateRepository(writeCtx);
            var writeUow = new EfUnitOfWork(writeCtx);
            await writeRepo.SaveSnapshotAsync(snapshot, TestContext.Current.CancellationToken);
            await writeUow.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Read through a fresh context — simulates a separate request
        // and rules out change-tracker leakage masking a wiring bug.
        RateSnapshot? loaded;
        await using (var readCtx = NewContext())
        {
            var readRepo = new EfExchangeRateRepository(readCtx);
            loaded = await readRepo.GetSnapshotAsync(
                usd,
                asOf,
                TestContext.Current.CancellationToken
            );
        }

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Base.Should().Be(usd);
        loaded.AsOf.Should().Be(asOf);
        loaded.Rates.Should().HaveCount(2);
        loaded.Rates.Should().Contain(r => r.Quote == eur && r.Rate == 0.92m);
        loaded.Rates.Should().Contain(r => r.Quote == gbp && r.Rate == 0.79m);
    }
}
