using CurrencyTracker.Domain.Events;

namespace CurrencyTracker.Domain.UnitTests.Events;

/// <summary>
/// Tests covering the structural-equality semantics of the
/// <see cref="AlertTriggered"/> domain event record. The event has no
/// behaviour; the tests pin the equality contract that the Phase 12
/// notification dispatcher will rely on for deduplication and routing.
/// </summary>
public sealed class AlertTriggeredTests
{
    private static readonly Guid TestAlertId = Guid.Parse("44cd35f2-c253-44fe-85bb-8ae6d96fcf43");
    private static readonly Guid TestRuleId = Guid.Parse("6aebd08e-c13f-4d26-b732-2e55f75f5963");
    private static readonly DateTimeOffset TestFiredAt = new(2026, 5, 23, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Construction_assigns_all_fields()
    {
        var sut = new AlertTriggered(
            TestAlertId,
            TestRuleId,
            ObservedChangePercent: 2.15m,
            TestFiredAt
        );

        sut.AlertId.Should().Be(TestAlertId);
        sut.RuleId.Should().Be(TestRuleId);
        sut.ObservedChangePercent.Should().Be(2.15m);
        sut.FiredAt.Should().Be(TestFiredAt);
    }

    [Fact]
    public void Two_events_with_the_same_fields_are_equal()
    {
        var a = new AlertTriggered(TestAlertId, TestRuleId, 2.15m, TestFiredAt);
        var b = new AlertTriggered(TestAlertId, TestRuleId, 2.15m, TestFiredAt);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Two_events_with_different_alert_id_are_not_equal()
    {
        var otherAlertId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var a = new AlertTriggered(TestAlertId, TestRuleId, 2.15m, TestFiredAt);
        var b = new AlertTriggered(otherAlertId, TestRuleId, 2.15m, TestFiredAt);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Two_events_with_different_rule_id_are_not_equal()
    {
        var otherRuleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var a = new AlertTriggered(TestAlertId, TestRuleId, 2.15m, TestFiredAt);
        var b = new AlertTriggered(TestAlertId, otherRuleId, 2.15m, TestFiredAt);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Two_events_with_different_observed_change_percent_are_not_equal()
    {
        var a = new AlertTriggered(TestAlertId, TestRuleId, 2.15m, TestFiredAt);
        var b = new AlertTriggered(TestAlertId, TestRuleId, 2.25m, TestFiredAt);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Two_events_with_different_fired_at_are_not_equal()
    {
        var a = new AlertTriggered(TestAlertId, TestRuleId, 2.15m, TestFiredAt);
        var b = new AlertTriggered(TestAlertId, TestRuleId, 2.15m, TestFiredAt.AddSeconds(1));

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equal_events_have_the_same_hash_code()
    {
        var a = new AlertTriggered(TestAlertId, TestRuleId, 2.15m, TestFiredAt);
        var b = new AlertTriggered(TestAlertId, TestRuleId, 2.15m, TestFiredAt);

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
