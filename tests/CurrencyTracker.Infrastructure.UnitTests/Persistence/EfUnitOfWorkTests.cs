using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CurrencyTracker.Infrastructure.UnitTests.Persistence;

/// <summary>Unit tests for <see cref="EfUnitOfWork"/>.</summary>
public sealed class EfUnitOfWorkTests
{
    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );

    [Fact]
    public async Task SaveChangesAsync_returns_count_of_persisted_entries()
    {
        // Arrange
        await using var ctx = NewContext();
        ctx.Currencies.Add(
            Currency.Create(CurrencyCode.Create("USD").Value, "United States Dollar", 840).Value
        );
        ctx.Currencies.Add(Currency.Create(CurrencyCode.Create("EUR").Value, "Euro", 978).Value);
        var uow = new EfUnitOfWork(ctx);

        // Act
        var written = await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        written.Should().Be(2);
    }
}
