using CurrencyTracker.Application.Abstractions.Time;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// Deterministic test clock for unit tests.
/// </summary>
public sealed class FixedDateTimeProvider : IDateTimeProvider
{
    private DateTimeOffset _utcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDateTimeProvider"/> class.
    /// </summary>
    /// <param name="initialValue">The initial UTC date and time value.</param>
    public FixedDateTimeProvider(DateTimeOffset initialValue)
    {
        _utcNow = initialValue;
    }

    /// <summary>
    /// Gets the current fixed UTC date and time.
    /// </summary>
    public DateTimeOffset UtcNow => _utcNow;

    /// <summary>
    /// Moves the clock forward or backward by the provided delta.
    /// </summary>
    /// <param name="delta">The amount of time to add.</param>
    public void Advance(TimeSpan delta)
    {
        _utcNow = _utcNow.Add(delta);
    }

    /// <summary>
    /// Sets the clock to an exact UTC date and time.
    /// </summary>
    /// <param name="moment">The UTC date and time to set.</param>
    public void Set(DateTimeOffset moment)
    {
        _utcNow = moment;
    }
}
