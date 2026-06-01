using CurrencyTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CurrencyTracker.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Shared fixture: spins up a Postgres container once for all tests
/// in the consuming class, applies the InitialCreate migration, and
/// exposes the connection string. Disposed after the class's tests
/// finish.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.4")
        .WithDatabase("currencytracker")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    /// <summary>Gets the Postgres connection string. Valid after <see cref="InitializeAsync"/>.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // Apply migrations against the freshly-started container.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var ctx = new ApplicationDbContext(options);
        await ctx.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
