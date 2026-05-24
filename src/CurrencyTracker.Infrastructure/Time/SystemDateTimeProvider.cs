using CurrencyTracker.Application.Abstractions.Time;

namespace CurrencyTracker.Infrastructure.Time;

/// <summary>
/// Returns the system UTC clock.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time from the system clock.
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
