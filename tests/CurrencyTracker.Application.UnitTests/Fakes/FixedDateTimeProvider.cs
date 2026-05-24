using CurrencyTracker.Application.Abstractions.Time;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// Deterministic test clock for unit tests.
/// </summary>
public sealed class FixedDateTimeProvider : IDateTimeProvider
{
    private DateTimeOffset utcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDateTimeProvider"/> class.
    /// </summary>
    /// <param name="initialValue">The initial UTC date and time value.</param>
    public FixedDateTimeProvider(DateTimeOffset initialValue)
    {
        utcNow = initialValue;
    }

    /// <summary>
    /// Gets the current fixed UTC date and time.
    /// </summary>
    public DateTimeOffset UtcNow => utcNow;

    /// <summary>
    /// Moves the clock forward or backward by the provided delta.
    /// </summary>
    /// <param name="delta">The amount of time to add.</param>
    public void Advance(TimeSpan delta)
    {
        utcNow = utcNow.Add(delta);
    }

    /// <summary>
    /// Sets the clock to an exact UTC date and time.
    /// </summary>
    /// <param name="moment">The UTC date and time to set.</param>
    public void Set(DateTimeOffset moment)
    {
        utcNow = moment;
    }
}
