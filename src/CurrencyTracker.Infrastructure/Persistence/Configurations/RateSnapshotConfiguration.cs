using CurrencyTracker.Domain.Rates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CurrencyTracker.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for the <see cref="RateSnapshot"/>
/// aggregate root. <see cref="RateSnapshot.Rates"/> is mapped as an
/// owned-type collection (Phase 3.5 / 3.11 decision) — the child
/// <see cref="ExchangeRate"/> entries have no <c>DbSet</c> and are
/// reachable only via the snapshot's navigation property.
/// </summary>
internal sealed class RateSnapshotConfiguration : IEntityTypeConfiguration<RateSnapshot>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RateSnapshot> builder)
    {
        builder.ToTable("rate_snapshots");

        // Composite primary key matching the aggregate's identity.
        builder.HasKey(s => new { s.Base, s.AsOf });

        builder
            .Property(s => s.Base)
            .HasColumnName("base")
            .HasConversion(ValueConverters.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(s => s.AsOf).HasColumnName("as_of").IsRequired();

        builder.OwnsMany(
            s => s.Rates,
            rates =>
            {
                rates.ToTable("exchange_rates");

                rates.WithOwner().HasForeignKey("snapshot_base", "snapshot_as_of");

                // Composite key: (FK to snapshot) + Quote. The Quote
                // segment of the key gives Postgres a UNIQUE-style
                // guard against two rates with the same quote inside
                // the same snapshot — mirroring the SNAPSHOT_DUPLICATE_QUOTE
                // invariant from Phase 3.5.
                rates.HasKey("snapshot_base", "snapshot_as_of", "Quote");

                rates
                    .Property(r => r.Base)
                    .HasColumnName("base")
                    .HasConversion(ValueConverters.CurrencyCode)
                    .HasMaxLength(3)
                    .IsRequired();

                rates
                    .Property(r => r.Quote)
                    .HasColumnName("quote")
                    .HasConversion(ValueConverters.CurrencyCode)
                    .HasMaxLength(3)
                    .IsRequired();

                rates.Property(r => r.Rate).HasColumnName("rate").HasPrecision(18, 8).IsRequired();

                rates.Property(r => r.AsOf).HasColumnName("as_of").IsRequired();
            }
        );

        // The Rates navigation is backed by a private list field on
        // the aggregate. EF Core can construct the snapshot via a
        // constructor that accepts the field's contents — this
        // mapping is handled by EF Core's discovery against the
        // private constructor on RateSnapshot (Phase 3.5).
        builder.Navigation(s => s.Rates).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
