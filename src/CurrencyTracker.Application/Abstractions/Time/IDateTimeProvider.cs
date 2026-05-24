namespace CurrencyTracker.Application.Abstractions.Time;

/// <summary>
/// Provides the current UTC date and time.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
