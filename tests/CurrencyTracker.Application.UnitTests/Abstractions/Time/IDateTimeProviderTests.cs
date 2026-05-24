using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Infrastructure.Time;

namespace CurrencyTracker.Application.UnitTests.Abstractions.Time;

/// <summary>
/// Tests for <see cref="CurrencyTracker.Application.Abstractions.Time.IDateTimeProvider"/>.
/// </summary>
public sealed class IDateTimeProviderTests
{
    [Fact]
    public void FixedDateTimeProvider_uses_constructor_value_as_initial_time()
    {
        var expected = new DateTimeOffset(2026, 05, 24, 06, 00, 00, TimeSpan.Zero);
        var sut = new FixedDateTimeProvider(expected);

        sut.UtcNow.Should().Be(expected);
    }

    [Fact]
    public void FixedDateTimeProvider_Advance_moves_the_clock()
    {
        var start = new DateTimeOffset(2026, 05, 24, 06, 00, 00, TimeSpan.Zero);
        var sut = new FixedDateTimeProvider(start);

        sut.Advance(TimeSpan.FromMinutes(90));

        sut.UtcNow.Should().Be(start.AddMinutes(90));
    }

    [Fact]
    public void FixedDateTimeProvider_Set_jumps_to_a_new_time()
    {
        var start = new DateTimeOffset(2026, 05, 24, 06, 00, 00, TimeSpan.Zero);
        var target = new DateTimeOffset(2027, 01, 01, 00, 00, 00, TimeSpan.Zero);
        var sut = new FixedDateTimeProvider(start);

        sut.Set(target);

        sut.UtcNow.Should().Be(target);
    }

    [Fact]
    public void SystemDateTimeProvider_UtcNow_is_within_captured_bounds()
    {
        var sut = new SystemDateTimeProvider();
        var before = DateTimeOffset.UtcNow;

        var utcNow = sut.UtcNow;

        var after = DateTimeOffset.UtcNow;
        utcNow.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
