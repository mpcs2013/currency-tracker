namespace CurrencyTracker.Domain.UnitTests;

/// <summary>
/// Smoke tests confirming xUnit v3 discovery and FluentAssertions
/// chains compile and run. No production code is exercised; the
/// first real Domain tests land in Phase 3.
/// </summary>
public sealed class SmokeTests
{
    /// <summary>
    /// Verifies that xUnit v3 can discover and run a simple fact.
    /// </summary>
    [Fact]
    public void Xunit_v3_discovers_and_runs_a_fact()
    {
        var sut = true;

        sut.Should().BeTrue("xUnit v3 + FluentAssertions are wired correctly");
    }
}
