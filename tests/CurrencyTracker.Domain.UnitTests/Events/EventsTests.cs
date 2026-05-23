using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Events;

namespace CurrencyTracker.Domain.UnitTests.Events;

/// <summary>
/// Tests covering structural equality semantics for domain event records.
/// </summary>
public sealed class EventsTests
{
    [Fact]
    public void Two_rate_ingested_events_with_the_same_fields_are_equal()
    {
        var @base = CurrencyCode.Create("USD").Value;
        var asOf = new DateOnly(2026, 05, 23);
        const int rateCount = 12;

        var a = new RateIngested(@base, asOf, rateCount);
        var b = new RateIngested(@base, asOf, rateCount);

        a.Should().Be(b);
    }

    [Fact]
    public void Two_rate_ingested_events_with_different_rate_counts_are_not_equal()
    {
        var @base = CurrencyCode.Create("USD").Value;
        var asOf = new DateOnly(2026, 05, 23);

        var a = new RateIngested(@base, asOf, 12);
        var b = new RateIngested(@base, asOf, 13);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Two_alert_triggered_events_with_the_same_fields_are_equal()
    {
        var alertId = Guid.Parse("44cd35f2-c253-44fe-85bb-8ae6d96fcf43");
        var ruleId = Guid.Parse("6aebd08e-c13f-4d26-b732-2e55f75f5963");
        var firedAt = new DateTimeOffset(2026, 05, 23, 13, 00, 00, TimeSpan.Zero);
        const decimal observedChangePercent = 2.15m;

        var a = new AlertTriggered(alertId, ruleId, observedChangePercent, firedAt);
        var b = new AlertTriggered(alertId, ruleId, observedChangePercent, firedAt);

        a.Should().Be(b);
    }

    [Fact]
    public void Two_alert_triggered_events_with_different_observed_change_percents_are_not_equal()
    {
        var alertId = Guid.Parse("44cd35f2-c253-44fe-85bb-8ae6d96fcf43");
        var ruleId = Guid.Parse("6aebd08e-c13f-4d26-b732-2e55f75f5963");
        var firedAt = new DateTimeOffset(2026, 05, 23, 13, 00, 00, TimeSpan.Zero);

        var a = new AlertTriggered(alertId, ruleId, 2.15m, firedAt);
        var b = new AlertTriggered(alertId, ruleId, 2.25m, firedAt);

        a.Should().NotBe(b);
    }
}
