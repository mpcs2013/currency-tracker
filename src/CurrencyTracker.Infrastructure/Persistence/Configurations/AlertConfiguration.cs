using CurrencyTracker.Domain.Alerts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CurrencyTracker.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for the <see cref="Alert"/> entity —
/// an immutable record of a single <see cref="AlertRule"/> firing.
/// </summary>
internal sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.RuleId).IsRequired();
        builder.HasIndex(a => a.RuleId);

        builder.Property(a => a.PreviousRate).HasPrecision(18, 8).IsRequired();
        builder.Property(a => a.CurrentRate).HasPrecision(18, 8).IsRequired();
        builder.Property(a => a.ObservedChangePercent).HasPrecision(5, 2).IsRequired();

        builder.Property(a => a.FiredAt).IsRequired();
    }
}
