using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the EF Core tools (`dotnet ef migrations
/// add`, `dotnet ef database update`). Returns a fully-configured
/// <see cref="ApplicationDbContext"/> without booting the Api host —
/// the host's <c>AddInfrastructure</c> extension (Phase 8.4) fail-fasts
/// on a missing <c>currencytracker</c> connection string, and the
/// design-time tools run outside the Aspire AppHost.
/// </summary>
/// <remarks>
/// EF Core's design-time host looks for an
/// <see cref="IDesignTimeDbContextFactory{TContext}"/> implementation
/// in the migrations assembly via reflection. When one is present it is
/// used in preference to the <c>HostFactoryResolver</c>-based fallback
/// that runs the Api's <c>Program.Main</c> — which is what would
/// otherwise hit the fail-fast.
/// <para>
/// The connection string here is a design-time placeholder.
/// <c>dotnet ef migrations add</c> generates SQL from the EF Core model
/// (the <c>IEntityTypeConfiguration&lt;T&gt;</c> files from 8.3); it
/// never opens a connection. The actual migration application happens
/// in &lt;see cref="MigrationRunner"/> (Phase 8.5, Development only) and
/// in the Phase 14 deploy pipeline (Production) — both read the real
/// connection string from &lt;see cref="IConfiguration"/>.
/// </para>
/// </remarks>
public sealed class ApplicationDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <inheritdoc />
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=currencytracker-design-time;Username=postgres;Password=postgres"
            )
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ApplicationDbContext(options);
    }
}
