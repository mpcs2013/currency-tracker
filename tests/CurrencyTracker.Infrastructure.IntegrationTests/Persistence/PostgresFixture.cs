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

    /// <summary>
    /// Truncates every application and Wolverine table so a shared-container
    /// test class can start each test from a clean slate. The schema itself
    /// (and the EF migrations-history table) is preserved. Wolverine's
    /// envelope tables are created lazily by the first host that starts, so
    /// the truncate is driven off <c>pg_tables</c> and silently skips tables
    /// that do not exist yet.
    /// </summary>
    public async Task ResetAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var ctx = new ApplicationDbContext(options);
        await ctx.Database.ExecuteSqlRawAsync(
            """
            DO $$
            DECLARE stmt text;
            BEGIN
                SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
                INTO stmt
                FROM pg_tables
                WHERE schemaname IN ('public', 'wolverine')
                  AND left(tablename, 2) <> '__';
                IF stmt IS NOT NULL THEN
                    EXECUTE 'TRUNCATE TABLE ' || stmt || ' RESTART IDENTITY CASCADE';
                END IF;
            END $$;
            """
        );
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
