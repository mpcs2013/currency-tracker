using CurrencyTracker.Domain.Currencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CurrencyTracker.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for the <see cref="Currency"/>
/// entity. Identity is the ISO 4217 alphabetic code; the
/// <see cref="CurrencyCode"/> value object is value-converted to a
/// 3-character string on write and read.
/// </summary>
internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies");

        builder.HasKey(c => c.Code);

        builder
            .Property(c => c.Code)
            .HasConversion(ValueConverters.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();

        builder.Property(c => c.NumericCode).IsRequired();
    }
}
