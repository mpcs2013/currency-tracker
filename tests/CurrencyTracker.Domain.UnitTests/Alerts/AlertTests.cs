using CurrencyTracker.Domain.Alerts;

namespace CurrencyTracker.Domain.UnitTests.Alerts;

/// <summary>
/// Tests covering <see cref="Alert"/> creation validation,
/// computed <see cref="Alert.ObservedChangePercent"/>, and
/// identity-based equality semantics.
/// </summary>
public sealed class AlertTests
{
    private static readonly Guid TestRuleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset TestFiredAt = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_valid_values_returns_success()
    {
        var result = Alert.Create(TestRuleId, 1.00m, 1.05m, TestFiredAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.RuleId.Should().Be(TestRuleId);
        result.Value.PreviousRate.Should().Be(1.00m);
        result.Value.CurrentRate.Should().Be(1.05m);
        result.Value.FiredAt.Should().Be(TestFiredAt);
    }

    [Fact]
    public void Create_empty_ruleId_returns_failure_with_ALERT_RULE_REQUIRED()
    {
        var result = Alert.Create(Guid.Empty, 1.00m, 1.05m, TestFiredAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ALERT_RULE_REQUIRED");
    }

    [Fact]
    public void Create_non_positive_previousRate_returns_failure_with_ALERT_PREVIOUS_RATE_NONPOSITIVE()
    {
        var result = Alert.Create(TestRuleId, 0m, 1.05m, TestFiredAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ALERT_PREVIOUS_RATE_NONPOSITIVE");
    }

    [Fact]
    public void Create_negative_currentRate_returns_failure_with_ALERT_CURRENT_RATE_NEGATIVE()
    {
        var result = Alert.Create(TestRuleId, 1.00m, -0.01m, TestFiredAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ALERT_CURRENT_RATE_NEGATIVE");
    }

    [Fact]
    public void Create_default_firedAt_returns_failure_with_ALERT_FIRED_AT_REQUIRED()
    {
        var result = Alert.Create(TestRuleId, 1.00m, 1.05m, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ALERT_FIRED_AT_REQUIRED");
    }

    [Fact]
    public void Create_computes_observed_change_percent_correctly()
    {
        // 1.00 → 1.05 is a 5% change.
        var result = Alert.Create(TestRuleId, 1.00m, 1.05m, TestFiredAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.ObservedChangePercent.Should().Be(5.00m);
    }

    [Fact]
    public void Create_computes_observed_change_percent_as_absolute_for_decreasing_rate()
    {
        // 1.00 → 0.90 is a -10% change; stored as absolute 10%.
        var result = Alert.Create(TestRuleId, 1.00m, 0.90m, TestFiredAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.ObservedChangePercent.Should().Be(10.00m);
    }

    [Fact]
    public void Two_alerts_with_same_id_are_equal()
    {
        var alert = Alert.Create(TestRuleId, 1.00m, 1.05m, TestFiredAt).Value;

        alert.Should().Be(alert);
    }

    [Fact]
    public void Two_alerts_with_different_ids_are_not_equal()
    {
        var a = Alert.Create(TestRuleId, 1.00m, 1.05m, TestFiredAt).Value;
        var b = Alert.Create(TestRuleId, 1.00m, 1.05m, TestFiredAt).Value;

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equal_alerts_have_the_same_hash_code()
    {
        var alert = Alert.Create(TestRuleId, 1.00m, 1.05m, TestFiredAt).Value;

        alert.GetHashCode().Should().Be(alert.GetHashCode());
    }
}
