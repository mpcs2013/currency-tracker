namespace CurrencyTracker.Application.UnitTests;

/// <summary>
/// Smoke tests confirming xUnit v3 + FluentAssertions + NSubstitute
/// are wired in this project. No production code is exercised; the
/// first real Application tests land in Phase 4 once ports exist.
/// </summary>
public sealed class SmokeTests
{
    [Fact]
    public void Xunit_v3_discovers_and_runs_a_fact()
    {
        var sut = true;

        sut.Should().BeTrue("xUnit v3 + FluentAssertions are wired correctly");
    }

    [Fact]
    public void NSubstitute_creates_a_substitute_for_an_interface()
    {
        var substitute = Substitute.For<IDisposable>();

        substitute.Should().NotBeNull("NSubstitute is wired correctly");
    }
}
