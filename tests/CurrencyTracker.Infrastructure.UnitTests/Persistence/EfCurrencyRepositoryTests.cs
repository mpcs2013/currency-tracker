using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CurrencyTracker.Infrastructure.UnitTests.Persistence;

/// <summary>Unit tests for <see cref="EfCurrencyRepository"/>.</summary>
public sealed class EfCurrencyRepositoryTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );

    private static Currency NewCurrency(CurrencyCode code, string name, int numeric) =>
        Currency.Create(code, name, numeric).Value;

    [Fact]
    public async Task GetAllAsync_returns_empty_when_no_currencies()
    {
        // Arrange
        await using var ctx = NewContext();
        var repo = new EfCurrencyRepository(ctx);

        // Act
        var result = await repo.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_returns_all_currencies_when_present()
    {
        // Arrange
        await using var ctx = NewContext();
        ctx.Currencies.AddRange(
            NewCurrency(Usd, "United States Dollar", 840),
            NewCurrency(Eur, "Euro", 978)
        );
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new EfCurrencyRepository(ctx);

        // Act
        var result = await repo.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Code == Usd);
        result.Should().Contain(c => c.Code == Eur);
    }

    [Fact]
    public async Task GetByCodeAsync_returns_currency_when_present()
    {
        // Arrange
        await using var ctx = NewContext();
        ctx.Currencies.Add(NewCurrency(Usd, "United States Dollar", 840));
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new EfCurrencyRepository(ctx);

        // Act
        var result = await repo.GetByCodeAsync(Usd, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(Usd);
    }

    [Fact]
    public async Task GetByCodeAsync_returns_null_when_absent()
    {
        // Arrange
        await using var ctx = NewContext();
        var repo = new EfCurrencyRepository(ctx);

        // Act
        var result = await repo.GetByCodeAsync(Usd, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_tracks_currency_for_insertion()
    {
        // Arrange
        await using var ctx = NewContext();
        var repo = new EfCurrencyRepository(ctx);
        var currency = NewCurrency(Usd, "United States Dollar", 840);

        // Act
        await repo.AddAsync(currency, TestContext.Current.CancellationToken);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var stored = await ctx.Currencies.FindAsync(
            new object?[] { Usd },
            TestContext.Current.CancellationToken
        );
        stored.Should().NotBeNull();
    }
}
