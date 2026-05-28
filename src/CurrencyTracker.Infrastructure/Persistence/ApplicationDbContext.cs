using CurrencyTracker.Domain.Alerts;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;
using Microsoft.EntityFrameworkCore;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// The single <see cref="DbContext"/> for the CurrencyTracker
/// persistence model. Bound to Postgres via the Npgsql provider; the
/// connection string is read from <c>IConfiguration</c> at DI-registration
/// time (see 8.4).
/// </summary>
/// <remarks>
/// There is intentionally no <c>DbSet&lt;ExchangeRate&gt;</c> property.
/// <see cref="ExchangeRate"/> is part of the <see cref="RateSnapshot"/>
/// aggregate (Phase 3.5) and is reachable only via the snapshot's
/// <see cref="RateSnapshot.Rates"/> navigation property. The owned-type
/// configuration in <c>RateSnapshotConfiguration</c> (Phase 8.3) makes
/// that explicit at the EF Core level.
/// </remarks>
public sealed class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">DbContext options including the connection
    /// string, configured in <c>Program.cs</c>.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    /// <summary>Gets the currency catalogue table.</summary>
    public DbSet<Currency> Currencies => Set<Currency>();

    /// <summary>
    /// Gets the rate-snapshot aggregate table. Each row owns a
    /// collection of <see cref="ExchangeRate"/> entries through its
    /// <see cref="RateSnapshot.Rates"/> navigation property.
    /// </summary>
    public DbSet<RateSnapshot> RateSnapshots => Set<RateSnapshot>();

    /// <summary>Gets the alert-rule table.</summary>
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();

    /// <summary>Gets the alert-firing-record table.</summary>
    public DbSet<Alert> Alerts => Set<Alert>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
