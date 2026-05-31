using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;
using CurrencyTracker.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CurrencyTracker.Infrastructure.UnitTests.Persistence;

/// <summary>
/// Unit tests for <see cref="EfExchangeRateRepository"/> against the
/// in-memory EF Core provider. The real-Postgres round trip is
/// covered separately by 8.9's <c>ExchangeRateRoundTrip</c> test
/// in <c>CurrencyTracker.Infrastructure.IntegrationTests</c>.
/// </summary>
public sealed class EfExchangeRateRepositoryTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;
    private static readonly CurrencyCode Gbp = CurrencyCode.Create("GBP").Value;
    private static readonly DateOnly AsOf = new(2026, 5, 27);

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );

    [Fact]
    public async Task GetSnapshotAsync_returns_null_when_no_snapshot_for_pair()
    {
        // Arrange
        await using var ctx = NewContext();
        var repo = new EfExchangeRateRepository(ctx);

        // Act
        var result = await repo.GetSnapshotAsync(Usd, AsOf, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSnapshotAsync_returns_snapshot_with_rates_when_present()
    {
        // Arrange
        await using var ctx = NewContext();
        var rates = new[]
        {
            ExchangeRate.Create(Usd, Eur, 0.92m, AsOf).Value,
            ExchangeRate.Create(Usd, Gbp, 0.79m, AsOf).Value,
        };
        var snapshot = RateSnapshot.Create(Usd, AsOf, rates).Value;
        ctx.RateSnapshots.Add(snapshot);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new EfExchangeRateRepository(ctx);

        // Act
        var loaded = await repo.GetSnapshotAsync(Usd, AsOf, TestContext.Current.CancellationToken);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Base.Should().Be(Usd);
        loaded.AsOf.Should().Be(AsOf);
        loaded.Rates.Should().HaveCount(2);
    }

    [Fact]
    public async Task SaveSnapshotAsync_persists_new_snapshot_with_rates()
    {
        // Arrange
        await using var ctx = NewContext();
        var repo = new EfExchangeRateRepository(ctx);
        var rates = new[] { ExchangeRate.Create(Usd, Eur, 0.92m, AsOf).Value };
        var snapshot = RateSnapshot.Create(Usd, AsOf, rates).Value;

        // Act
        await repo.SaveSnapshotAsync(snapshot, TestContext.Current.CancellationToken);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var stored = await ctx
            .RateSnapshots.Include(s => s.Rates)
            .FirstOrDefaultAsync(
                s => s.Base == Usd && s.AsOf == AsOf,
                TestContext.Current.CancellationToken
            );
        stored.Should().NotBeNull();
        stored!.Rates.Should().HaveCount(1);
        stored.Rates[0].Quote.Should().Be(Eur);
    }

    [Fact]
    public async Task SaveSnapshotAsync_replaces_existing_snapshot_with_same_key()
    {
        // Arrange
        await using var ctx = NewContext();
        var initial = RateSnapshot
            .Create(Usd, AsOf, [ExchangeRate.Create(Usd, Eur, 0.92m, AsOf).Value])
            .Value;
        ctx.RateSnapshots.Add(initial);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new EfExchangeRateRepository(ctx);
        var replacement = RateSnapshot
            .Create(
                Usd,
                AsOf,
                [
                    ExchangeRate.Create(Usd, Eur, 0.93m, AsOf).Value,
                    ExchangeRate.Create(Usd, Gbp, 0.80m, AsOf).Value,
                ]
            )
            .Value;

        // Act
        await repo.SaveSnapshotAsync(replacement, TestContext.Current.CancellationToken);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var stored = await ctx
            .RateSnapshots.Include(s => s.Rates)
            .FirstOrDefaultAsync(
                s => s.Base == Usd && s.AsOf == AsOf,
                TestContext.Current.CancellationToken
            );
        stored.Should().NotBeNull();
        stored!.Rates.Should().HaveCount(2);
        stored.Rates.Should().Contain(r => r.Quote == Eur && r.Rate == 0.93m);
        stored.Rates.Should().Contain(r => r.Quote == Gbp && r.Rate == 0.80m);
    }
}
