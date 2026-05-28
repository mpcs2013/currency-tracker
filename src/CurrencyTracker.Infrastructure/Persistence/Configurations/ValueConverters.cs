using CurrencyTracker.Domain.Currencies;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CurrencyTracker.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shared <see cref="ValueConverter"/> instances used by every
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> that maps a Phase 3 value
/// object to a Postgres column.
/// </summary>
internal static class ValueConverters
{
    /// <summary>
    /// Converts <see cref="CurrencyCode"/> to/from a 3-character
    /// <see cref="string"/>. The read-side uses
    /// <see cref="CurrencyCode.Create(string)"/> so a malformed value
    /// in the database — which the writes above can't produce, but a
    /// data-import script might — fails fast on read.
    /// </summary>
    public static readonly ValueConverter<CurrencyCode, string> CurrencyCode = new(
        code => code.Value,
        raw => Domain.Currencies.CurrencyCode.Create(raw).Value
    );
}
