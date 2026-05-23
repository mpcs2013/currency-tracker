using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Events;

namespace CurrencyTracker.Domain.UnitTests.Events;

/// <summary>
/// Tests covering the structural-equality semantics of the
/// <see cref="RateIngested"/> domain event record. The event has no
/// behaviour; the tests pin the equality contract that downstream
/// handlers (Phase 10's cache invalidator, Phase 12's alert evaluator)
/// will rely on.
/// </summary>
public sealed class RateIngestedTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;
    private static readonly DateOnly TestAsOf = new(2026, 5, 23);

    [Fact]
    public void Construction_assigns_all_fields()
    {
        var sut = new RateIngested(Usd, TestAsOf, RateCount: 12);

        sut.Base.Should().Be(Usd);
        sut.AsOf.Should().Be(TestAsOf);
        sut.RateCount.Should().Be(12);
    }

    [Fact]
    public void Two_events_with_the_same_fields_are_equal()
    {
        var a = new RateIngested(Usd, TestAsOf, 12);
        var b = new RateIngested(Usd, TestAsOf, 12);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Two_events_with_different_base_are_not_equal()
    {
        var a = new RateIngested(Usd, TestAsOf, 12);
        var b = new RateIngested(Eur, TestAsOf, 12);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Two_events_with_different_asof_are_not_equal()
    {
        var a = new RateIngested(Usd, TestAsOf, 12);
        var b = new RateIngested(Usd, TestAsOf.AddDays(1), 12);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Two_events_with_different_rate_counts_are_not_equal()
    {
        var a = new RateIngested(Usd, TestAsOf, 12);
        var b = new RateIngested(Usd, TestAsOf, 13);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equal_events_have_the_same_hash_code()
    {
        var a = new RateIngested(Usd, TestAsOf, 12);
        var b = new RateIngested(Usd, TestAsOf, 12);

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
