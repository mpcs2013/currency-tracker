using CurrencyTracker.Domain.Alerts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CurrencyTracker.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for the <see cref="AlertRule"/> entity.
/// Identity is a generated <see cref="Guid"/>; the
/// <see cref="AlertRule.OwnerId"/> column is indexed so the eventual
/// Phase 12 "alerts for this user" query is non-table-scanning.
/// </summary>
internal sealed class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("alert_rules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OwnerId).IsRequired();
        builder.HasIndex(r => r.OwnerId);

        builder
            .Property(r => r.Base)
            .HasConversion(ValueConverters.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder
            .Property(r => r.Quote)
            .HasConversion(ValueConverters.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(r => r.ThresholdPercent).HasPrecision(5, 2).IsRequired();

        builder.Property(r => r.Channel).HasConversion<int>().IsRequired();

        builder.Property(r => r.Enabled).IsRequired();
    }
}
