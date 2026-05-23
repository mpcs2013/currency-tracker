using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Domain.UnitTests.Rates;

/// <summary>
/// Tests covering <see cref="RateSnapshot"/> creation invariants
/// (base/asof consistency, duplicate-quote rejection, empty-set rejection),
/// the <see cref="RateSnapshot.TryGetRate"/> lookup, and identity-based
/// equality on the (<c>Base</c>, <c>AsOf</c>) pair.
/// </summary>
public sealed class RateSnapshotTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;
    private static readonly CurrencyCode Gbp = CurrencyCode.Create("GBP").Value;
    private static readonly CurrencyCode Jpy = CurrencyCode.Create("JPY").Value;
    private static readonly DateOnly TestAsOf = new(2026, 5, 23);

    private static ExchangeRate Rate(
        CurrencyCode baseCurrency,
        CurrencyCode quote,
        decimal rate,
        DateOnly? asOf = null
    ) => ExchangeRate.Create(baseCurrency, quote, rate, asOf ?? TestAsOf).Value;

    [Fact]
    public void Create_valid_rates_returns_success()
    {
        var rates = new[] { Rate(Usd, Eur, 0.92m), Rate(Usd, Gbp, 0.79m), Rate(Usd, Jpy, 152.30m) };

        var result = RateSnapshot.Create(Usd, TestAsOf, rates);

        result.IsSuccess.Should().BeTrue();
        result.Value.Base.Should().Be(Usd);
        result.Value.AsOf.Should().Be(TestAsOf);
        result.Value.Rates.Should().HaveCount(3);
    }

    [Fact]
    public void Create_empty_rates_returns_failure_with_SNAPSHOT_EMPTY()
    {
        var result = RateSnapshot.Create(Usd, TestAsOf, Array.Empty<ExchangeRate>());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SNAPSHOT_EMPTY");
    }

    [Fact]
    public void Create_rate_with_mismatched_base_returns_failure_with_SNAPSHOT_BASE_MISMATCH()
    {
        var rates = new[]
        {
            Rate(Usd, Eur, 0.92m),
            Rate(Eur, Gbp, 0.86m), // wrong base — should be USD
        };

        var result = RateSnapshot.Create(Usd, TestAsOf, rates);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SNAPSHOT_BASE_MISMATCH");
    }

    [Fact]
    public void Create_rate_with_mismatched_asof_returns_failure_with_SNAPSHOT_ASOF_MISMATCH()
    {
        var rates = new[]
        {
            Rate(Usd, Eur, 0.92m, TestAsOf),
            Rate(Usd, Gbp, 0.79m, TestAsOf.AddDays(1)), // wrong date
        };

        var result = RateSnapshot.Create(Usd, TestAsOf, rates);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SNAPSHOT_ASOF_MISMATCH");
    }

    [Fact]
    public void Create_duplicate_quote_returns_failure_with_SNAPSHOT_DUPLICATE_QUOTE()
    {
        var rates = new[]
        {
            Rate(Usd, Eur, 0.92m),
            Rate(Usd, Eur, 0.93m), // same quote twice — revision is not allowed inside one snapshot
        };

        var result = RateSnapshot.Create(Usd, TestAsOf, rates);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SNAPSHOT_DUPLICATE_QUOTE");
    }

    [Fact]
    public void Create_materialises_the_enumerable_once()
    {
        // A deferred enumerable that throws after the first iteration would
        // expose a multi-pass bug. We use a counting wrapper to assert exactly
        // one iteration over the source.
        var iterationCount = 0;
        IEnumerable<ExchangeRate> CountingSource()
        {
            iterationCount++;
            yield return Rate(Usd, Eur, 0.92m);
            yield return Rate(Usd, Gbp, 0.79m);
        }

        var result = RateSnapshot.Create(Usd, TestAsOf, CountingSource());

        result.IsSuccess.Should().BeTrue();
        iterationCount.Should().Be(1);
    }

    [Fact]
    public void Rates_is_exposed_as_read_only_list()
    {
        var rates = new[] { Rate(Usd, Eur, 0.92m) };
        var snapshot = RateSnapshot.Create(Usd, TestAsOf, rates).Value;

        // The compile-time type is the invariant; this assertion documents it
        // at runtime too. Attempts to cast to List<ExchangeRate> may succeed
        // for the current ReadOnlyCollection wrapper but the public surface
        // is IReadOnlyList<ExchangeRate>.
        snapshot.Rates.Should().BeAssignableTo<IReadOnlyList<ExchangeRate>>();
    }

    [Fact]
    public void TryGetRate_returns_true_and_outputs_rate_when_quote_is_present()
    {
        var eurRate = Rate(Usd, Eur, 0.92m);
        var snapshot = RateSnapshot
            .Create(Usd, TestAsOf, new[] { eurRate, Rate(Usd, Gbp, 0.79m) })
            .Value;

        var found = snapshot.TryGetRate(Eur, out var rate);

        found.Should().BeTrue();
        rate.Should().Be(eurRate);
    }

    [Fact]
    public void TryGetRate_returns_false_when_quote_is_absent()
    {
        var snapshot = RateSnapshot.Create(Usd, TestAsOf, new[] { Rate(Usd, Eur, 0.92m) }).Value;

        var found = snapshot.TryGetRate(Jpy, out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void Two_snapshots_with_same_base_and_asof_but_different_rates_are_equal()
    {
        var a = RateSnapshot.Create(Usd, TestAsOf, new[] { Rate(Usd, Eur, 0.92m) }).Value;
        var b = RateSnapshot
            .Create(Usd, TestAsOf, new[] { Rate(Usd, Eur, 0.93m), Rate(Usd, Gbp, 0.79m) })
            .Value;

        // Identity is (Base, AsOf) only. A revised snapshot for the same
        // base/date is the *same snapshot* in identity terms.
        a.Should().Be(b);
    }

    [Fact]
    public void Two_snapshots_with_different_base_are_not_equal()
    {
        var usdSnapshot = RateSnapshot.Create(Usd, TestAsOf, new[] { Rate(Usd, Eur, 0.92m) }).Value;
        var eurSnapshot = RateSnapshot.Create(Eur, TestAsOf, new[] { Rate(Eur, Usd, 1.09m) }).Value;

        usdSnapshot.Should().NotBe(eurSnapshot);
    }

    [Fact]
    public void Two_snapshots_with_different_asof_are_not_equal()
    {
        var today = RateSnapshot
            .Create(Usd, TestAsOf, new[] { Rate(Usd, Eur, 0.92m, TestAsOf) })
            .Value;
        var tomorrow = RateSnapshot
            .Create(Usd, TestAsOf.AddDays(1), new[] { Rate(Usd, Eur, 0.93m, TestAsOf.AddDays(1)) })
            .Value;

        today.Should().NotBe(tomorrow);
    }

    [Fact]
    public void Equal_snapshots_have_the_same_hash_code()
    {
        var a = RateSnapshot.Create(Usd, TestAsOf, new[] { Rate(Usd, Eur, 0.92m) }).Value;
        var b = RateSnapshot
            .Create(Usd, TestAsOf, new[] { Rate(Usd, Eur, 0.93m), Rate(Usd, Gbp, 0.79m) })
            .Value;

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
